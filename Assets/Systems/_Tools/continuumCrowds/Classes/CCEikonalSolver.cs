using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Priority_Queue;

// ******************************************************************************************
// 							THE EIKONAL SOLVER
// ******************************************************************************************
// 3 labels for each node (Location): far, considered, accepted
//	labels are tracked by:  far - has huge value (Mathf.Infinite)
//							considered - placed in a priorityQueue
//							accepted - stored in List<Location>
// The Algorithm:
//  1) set all nodes (xi=Ui=inf) to far, set nodes in goal (xi=Ui=0) to accepted
//  2) for each accepted node, use eikonal update formula to find new U', and
//		if U'<Ui, then Ui=U', and xi -> considered
// 	[proposed change to 2)]
//	2) instead of marking each node accepted, mark them considered, and begin the loop
//		They will naturally become accepted, as their value of 0 gives them highest priority.
//	The Loop:
//  -->	3) let xt be the considered node with smallest Ui
//	|  	4) for each neighbor (xi) of xt that is NOT accepted, calculate U'
//  |  	5) if U'<Ui, then Ui=U' and label xi as considered
//  ---	6) if there is a considered node, repeat from step 3
//
// To implement: considered nodes will be a priority queue, and the highest priority 
//				(lowest Ui in considered nodes)	will be pulled in step (3) each iteration

public class CCEikonalSolver {

	// Continuum Crowd fields (we're solving for these)
	public float[,] Phi;				// potential field
	public Vector2[,] dPhi;				// potential field gradient
	public Vector2[,] v;				// final velocity

	// Initiating fields (see if I can avoid storing these later)
	public Vector4[,] f;				// speed field
	public Vector4[,] C;				// Cost field
	public float [,] g;					// absolute discomfort 

	// public lists for the Eikonal solver
	bool[,] accepted, goal;
	FastPriorityQueue<fastLocation> considered;

	int N, M;			// store the dimensions for easy iteration

	// this array of Vect2's correlates to our data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	Vector2[] DIR_ENWS = new Vector2[] {Vector2.right, Vector2.up, Vector2.left, Vector2.down };

	public CCEikonalSolver(CC_Map_Package fields, List<Location> goalLocs) {
		f = fields.f;
		C = fields.C;
		g = fields.g;

		N = f.GetLength (0);
		M = f.GetLength (1);

		Phi = new float[N, M];
		dPhi = new Vector2[N, M];
		v = new Vector2[N, M];

		accepted = new bool[N,M];
		goal = new bool[N,M];

		considered = new FastPriorityQueue <fastLocation> (N*M);

		// calculate potential field (Eikonal solver)
		computePotentialField(fields, goalLocs) ;
		// calculate the gradient
		calculatePotentialGradientAndNormalize();
		// calculate velocity field
		calculateVelocityField();
	}


	void computePotentialField(CC_Map_Package fields, List<Location> goalLocs) {
		EikonalSolver(fields, goalLocs);
	}

	void EikonalSolver (CC_Map_Package fields, List<Location> goalLocs) {
		// start by assigning all values of potential a huge number to in-effect label them 'far'
		for (int n=0; n<N; n++) {
			for (int m=0; m<M; m++) {
				Phi[n,m] = Mathf.Infinity;
			}
		}

		// initiate by setting potential to 0 in the goal, and adding all goal locations to the considered list
		// goal locations will naturally become accepted nodes, as their 0-cost gives them 1st priority, and this
		// way, they are quickly added to accepted, while simultaneously propagating their lists and beginning the 
		// algorithm loop
		fastLocation loc;
		foreach (Location l in goalLocs) {
			if (isPointValid (l.x, l.y)) {
				Phi [l.x, l.y] = 0f;
				loc = new fastLocation (l.x, l.y);
				markGoal (loc);
				considered.Enqueue (loc, 0f);
			}
		}

		// THE EIKONAL UPDATE LOOP
		// next, we initiate the eikonal update loop, by initiating it with each goal point as 'considered'.
		// this will check each neighbor to see if it's a valid point (EikonalLocationValidityTest==true)
		// and if so, update if necessary
		while (considered.Count > 0f) {
			fastLocation current = considered.Dequeue();
			EikonalUpdateFormula(current);
			markAccepted(current);
		}
	}

	void EikonalUpdateFormula (fastLocation l) {
		float phi_proposed = Mathf.Infinity;
		int xInto, yInto;

		fastLocation neighbor;

		// cycle through directions to check all neighbors and perform the eikonal
		// update cycle on them
		for (int d = 0; d < DIR_ENWS.Length; d++) {
			xInto = l.x + (int)DIR_ENWS[d].x;
			yInto = l.y + (int)DIR_ENWS[d].y;

			neighbor = new fastLocation(xInto,yInto);

			if (isEikonalLocationValidAsNeighbor (neighbor)) {
				// The point is valid. Now, we pull values from THIS location's
				// 4 neighbors and use them in the calculation

				int xIInto, yIInto;
				float phi_mx, phi_my, C_mx, C_my;
				Vector4 phi_m;
				phi_m = Vector4.one * Mathf.Infinity;	

				// track cost of moving into each nearby space
				for (int dd = 0; dd < DIR_ENWS.Length; dd++) {
					xIInto = neighbor.x + (int)DIR_ENWS[dd].x;
					yIInto = neighbor.y + (int)DIR_ENWS[dd].y;

					if (isEikonalLocationValidToMoveInto (new fastLocation(xIInto, yIInto))) 
					{
						phi_m[dd] = Phi[xIInto,yIInto] + C[neighbor.x,neighbor.y][dd];
					}
				}
				// select out cheapest 
				phi_mx = Mathf.Min(phi_m[0],phi_m[2]);
				phi_my = Mathf.Min(phi_m[1],phi_m[3]);

				// now assign C_mx based on which direction was chosen
				if (phi_mx == phi_m[0]) {
					C_mx = C[neighbor.x,neighbor.y][0];
				} else {
					C_mx = C[neighbor.x,neighbor.y][2];
				}
				// now assign C_mx based on which direction was chosen
				if (phi_my == phi_m[1]) {
					C_my = C[neighbor.x,neighbor.y][1];
				} else {
					C_my = C[neighbor.x,neighbor.y][3];
				}

				// solve for our proposed Phi[neighbor] value using the quadratic solution to the
				// approximation of the eikonal equation
				float C_mx_Sq = C_mx*C_mx;
				float C_my_Sq = C_my*C_my;
				float phi_mDiff_Sq = (phi_mx - phi_my)*(phi_mx - phi_my);

				float valTest;
				//				valTest = C_mx_Sq + C_my_Sq - 1f/(C_mx_Sq*C_my_Sq);
				valTest = C_mx_Sq + C_my_Sq - 1f;

				// test the quadratic
				if (phi_mDiff_Sq > valTest) {
					// use the simplified solution for phi_proposed
					float phi_min = Mathf.Min(phi_mx,phi_my);
					float cost_min;
					if (phi_min==phi_mx) {cost_min = C_mx;}
					else {cost_min = C_my;}
					phi_proposed = cost_min + phi_min;
				} else {
					// solve the quadratic
					float radical = Mathf.Sqrt(C_mx_Sq*C_my_Sq*(C_mx_Sq + C_my_Sq - phi_mDiff_Sq));

					float soln1 = (C_my_Sq*phi_mx + C_mx_Sq*phi_my + radical) / (C_mx_Sq + C_my_Sq);
					float soln2 = (C_my_Sq*phi_mx + C_mx_Sq*phi_my - radical) / (C_mx_Sq + C_my_Sq);
					phi_proposed = Mathf.Max(soln1,soln2);
				}

				// we now have a phi_proposed

				// we are re-writing the phi-array real time, so we simply compare to the current slot
				if (phi_proposed < Phi[neighbor.x,neighbor.y]) {
					// save the value of the lower phi
					Phi[neighbor.x,neighbor.y] = phi_proposed;


					if (considered.Contains(neighbor)) {
						// re-write the old value in the queue
						considered.UpdatePriority(neighbor, phi_proposed);
					} else {
						// -OR- add this value to the queue
						considered.Enqueue(neighbor, Phi[neighbor.x,neighbor.y]);
					}
				}
			}
		}		
	}

	bool isEikonalLocationValidAsNeighbor (fastLocation l) {
		// A valid neighbor point is:
		//		1) not outisde the local grid
		//		3) NOT in the goal						(everything below this is checked elsewhere)
		//		2) NOT accepted
		//		4) NOT on a global discomfort grid		(this occurs in isPointValid() )
		//		5) NOT outside the global grid			(this occurs in isPointValid() )
		if (!isEikonalLocationInsideLocalGrid(l)) {return false;}
		if (isLocationInGoal(l)) {return false;}
		return (isEikonalLocationAcceptedandValid (l));
	}

	bool isEikonalLocationValidToMoveInto (fastLocation l) {
		// location must be tested to ensure that it does not attempt to assess a point
		// that is not valid to move into. a valid point is:
		//		1) not outisde the local grid
		//		2) NOT accepted
		//		3) NOT on a global discomfort grid		(this occurs in isPointValid() )
		//		4) NOT outside the global grid			(this occurs in isPointValid() )
		if (!isEikonalLocationInsideLocalGrid (l)) {return false;}
		return (isEikonalLocationAcceptedandValid (l));
	}

	bool isEikonalLocationInsideLocalGrid (fastLocation l) {
		if ((l.x < 0) || (l.y < 0) || (l.x > (N) - 1) || (l.y > (M) - 1)) {
			return false;
		}
		return true;
	}

	bool isEikonalLocationAcceptedandValid(fastLocation l) {
		if (isLocationAccepted(l)) {return false;}
		if (!isPointValid(l)) {return false;}
		return true;
	}

	void calculatePotentialGradientAndNormalize() {
		for (int i=0; i<(N); i++) {
			for (int k=0; k<(M); k++) {
				if ((i!=0) && (i!=(N)-1) && (k!=0) && (k!=(M)-1)) 
				{writeNormalizedPotentialGradientFieldData(i,k,i-1,i+1,k-1,k+1);} // generic spot
				else if ((i==0) && (k==(M)-1)) 			{writeNormalizedPotentialGradientFieldData(i,k,i,i+1,k-1,k);} 	// upper left corner
				else if ((i==(N)-1) && (k==0)) 			{writeNormalizedPotentialGradientFieldData(i,k,i-1,i,k,k+1);}	// bottom left corner
				else if ((i==0) && (k==0)) 				{writeNormalizedPotentialGradientFieldData(i,k,i,i+1,k,k+1);}	// upper left corner
				else if ((i==(N)-1) && (k==(M)-1)) 
				{writeNormalizedPotentialGradientFieldData(i,k,i-1,i,k-1,k);} 	// bottom right corner
				else if (i==0) 							{writeNormalizedPotentialGradientFieldData(i,k,i,i+1,k-1,k+1);}	// top edge
				else if (i==(N)-1) 						{writeNormalizedPotentialGradientFieldData(i,k,i-1,i,k-1,k+1);}	// bot edge
				else if (k==0) 							{writeNormalizedPotentialGradientFieldData(i,k,i-1,i+1,k,k+1);}	// left edge
				else if (k==(M)-1) 						{writeNormalizedPotentialGradientFieldData(i,k,i-1,i+1,k-1,k);}	// right edge								
			}
		}
	}

	void writeNormalizedPotentialGradientFieldData(int x, int y, int xMin, int xMax, int yMin, int yMax) {
		float phiXmin = Phi[xMin,y], phiXmax = Phi[xMax,y], phiYmin = Phi[x,yMin], phiYmax = Phi[x,yMax];
		float dPhidx, dPhidy;

		dPhidx = (phiXmax - phiXmin) / (xMax - xMin);
		dPhidy = (phiYmax - phiYmin) / (yMax - yMin);

		if(float.IsInfinity(phiXmin) && float.IsInfinity(phiXmax)) {
			dPhidx = 0f;
		} else if(float.IsInfinity(phiXmin) || float.IsInfinity(phiXmax)) {
			dPhidx = Mathf.Sign(phiXmax - phiXmin);
		}

		if(float.IsInfinity(phiYmin) && float.IsInfinity(phiYmax)) {
			dPhidy = 0f;
		} else if(float.IsInfinity(phiYmin) || float.IsInfinity(phiYmax)) {
			dPhidy = Mathf.Sign(phiYmax - phiYmin);
		}

		dPhi[x,y] = (new Vector2(dPhidx, dPhidy)).normalized;
	}

	void calculateVelocityField() {
		float vx, vy;

		for (int i=0; i<(N); i++) {
			for (int k=0; k<(M); k++) {

				if (dPhi[i,k].x > 0) {
					vx = -f[i,k][2] * dPhi[i,k].x;
				} else {
					vx = -f[i,k][0] * dPhi[i,k].x;
				}

				if (dPhi[i,k].y > 0) {
					vy = -f[i,k][3] * dPhi[i,k].y;
				} else {
					vy = -f[i,k][1] * dPhi[i,k].y;
				}

				v[i,k] = (new Vector2(vx,vy));
			}
		}
	}

	void markAccepted(fastLocation l) {
		accepted[l.x,l.y] = true;
	}
	bool isLocationAccepted(fastLocation l) {
		return accepted[l.x,l.y];
	}
	void markGoal(fastLocation l) {
		goal[l.x,l.y] = true;
	}
	bool isLocationInGoal(fastLocation l) {
		return goal[l.x,l.y];
	}	

	bool isPointValid (fastLocation l) {return isPointValid (l.x, l.y);}
	bool isPointValid (Vector2 v)
	{
		return isPointValid((int)v.x, (int)v.y);
	}
	bool isPointValid (int x, int y)
	{
		// check to make sure the point is not outside the grid
		if ((x < 0) || (y < 0) || (x > N - 1) || (y > M - 1)) {
			return false;
		}
		// check to make sure the point is not on a place of absolute discomfort (like inside a building)
		// check to make sure the point is not in a place dis-allowed by terrain (slope)
		if (g[x,y]==1) {return false;}

		return true;
	}
}