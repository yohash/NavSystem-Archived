using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Priority_Queue;

public class fastLocation : FastPriorityQueueNode
{
	public readonly int x, y;
	public fastLocation(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	public static bool operator ==(fastLocation l1, fastLocation l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool Equals(fastLocation l1, fastLocation l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool operator !=(fastLocation l1, fastLocation l2) {
		return(!(l1==l2));
	}
}

public struct CC_Unit_Goal_Group
{
	public Rect goal;
	public List<CC_Unit> units;

	public CC_Unit_Goal_Group (Rect r, List<CC_Unit> u)
	{
		this.goal = r;
		this.units = u;
	}
}

public struct CC_Map_Package
{
	public Vector2[,] dh;
	public float[,] h;
	public float[,] g;

	public CC_Map_Package (Vector2[,] _dh, float[,] _h, float[,] _g)
	{
		this.dh = _dh;
		this.h = _h;
		this.g = _g;
	}
}

public class ContinuumCrowds
{
	private float TIMESTAMP, dT, subTot;

	// the Continuum Crowds fields
	public float[,] rho;				// density field
	public Vector2[,] vAve;				// average velocity field
	public float[,] gP;					// predictive discomfort
	public Vector4[,] f;				// speed field, data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	public Vector4[,] C;				// cost field, data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	public float[,] Phi;				// potential field
	public Vector2[,] dPhi;				// potential field gradient
	public Vector2[,] v;				// final velocity

	public List<Vector2[,]> vFields;	// all the final fields encapsulated

	// parameters for the respective equations -- FIND MORE APPROPRIATE PLACE TO PUT THESE
	public float rho_sc = 1.2f;					// scalar for density to splat onto discomfort map

	public float gP_predictiveSeconds = 4f;		// how far into future we predict the path
	public float gP_weight = 2f;				// scalar for predictive discomfort to splat onto discomfort map

	public float f_slopeMax = 1f;				// correlates roughly to 30-degree incline
	public float f_slopeMin = 0f;				// for a min slope, nothing else makes sense...

	public float f_rhoMax = 0.6f;				// TODO: everything above this must be clamped to 'unpassable' discomfort map
	public float f_rhoMin = 0.2f;

	public float f_speedMax = 10f;				// will this vary by unit???
	public float f_speedMin = 0.01f;			// set to some positive number to clamp flow speed

	public float C_alpha = 8f;					// speed field weight
	public float C_beta = 1f;					// time weight
	public float C_gamma = 2f;					// discomfort weight


	// public lists for the Eikonal solver
	bool[,] accepted, goal;
	FastPriorityQueue<fastLocation> considered;

	// These are the two input values
	CC_Map_Package theMap;
	List<CC_Unit_Goal_Group> theUnitGoalGroups;

	// this array of Vect2's correlates to our data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	Vector2[] DIR_ENWS = new Vector2[] {Vector2.right, Vector2.up, Vector2.left, Vector2.down };

	int N, M;			// store the dimensions for easy iteration
	int Nloc, Mloc;		// store local location for eikonal iterations
	int Ndim, Mdim;		// store local dimension for eikonal iterations

	public ContinuumCrowds (CC_Map_Package map, List<CC_Unit_Goal_Group> unitGoals)
	{
		theMap = map;
		theUnitGoalGroups = unitGoals;

		N = map.h.GetLength (0);
		M = map.h.GetLength (1);

		rho = new float[N, M];
		vAve = new Vector2[N, M];
		gP = new float[N, M];
		f = new Vector4[N, M];
		C = new Vector4[N, M];

		vFields = new List<Vector2[,]>();

		// these next fields must be computed for each unit in the entire list:
		// 		populate density field
		// 		populate average velocity field
		// 		populate predictive discomfort field
		// 		populate speed field
		foreach (CC_Unit_Goal_Group cc_ugg in theUnitGoalGroups) {
			foreach (CC_Unit cc_u in cc_ugg.units) {
				// (1) density field and velocity
				computeDensityField (cc_u); 		
				// (2) predictive discomfort field
				applyPredictiveDiscomfort (gP_predictiveSeconds, cc_u);	
			}
		}
		// (3) 	now that the velocity field and density fields are implemented,
		// 		divide the velocity by density to get average velocity field
		computeAverageVelocityField ();	

		// (4)	now that the average velocity field is computed, and the density
		// 		field is in place, we calculate the speed field, f
		// **** FOR NOW, LETS CALCLUATE THE WHOLE THING. LATER, WE SHOULD SAVE COMPUTATION
		// **** BY ONLY CALCULATING THE NECESSARY POINTS USING THE COST FUNCTION
		computeSpeedField ();

		// (5) 	the cost field depends only on f and g, so it can be computed in its
		//		entirety now as well
		computeCostField();

		// these next fields must be computed for each goal in the entire list:
		Rect soln;
		foreach (CC_Unit_Goal_Group cc_ugg in unitGoals) {

			Phi = new float[N, M];
			dPhi = new Vector2[N, M];
			v = new Vector2[N, M];

			// determine the Rect to bound the current units and their goal
			soln = new Rect(Vector2.zero, new Vector2(N,M));
			// calculate potential field (Eikonal solver)
			computePotentialField(soln, cc_ugg);
			calculatePotentialGradientAndNormalize();
			// calculate velocity field
			calculateVelocityField();
			// add the vfield to the list for this particular Unit-Goal-Group
			vFields.Add(v);		
		}
	}

	void computeDensityField (CC_Unit cc_u)
	{
		Vector2 cc_u_pos = cc_u.getLocalPosition ();

		linear1stOrderSplat (cc_u_pos.x, cc_u_pos.y, rho, rho_sc);
		linear1stOrderSplat (cc_u_pos.x, cc_u_pos.y, gP, rho_sc);

		int xInd = (int)Mathf.Floor (cc_u_pos.x);
		int yInd = (int)Mathf.Floor (cc_u_pos.y);

		computeVelocityFieldPoint (xInd, yInd, cc_u.getVelocity ());
		computeVelocityFieldPoint (xInd + 1, yInd, cc_u.getVelocity ());
		computeVelocityFieldPoint (xInd, yInd + 1, cc_u.getVelocity ());
		computeVelocityFieldPoint (xInd + 1, yInd + 1, cc_u.getVelocity ());
	}

	void computeVelocityFieldPoint (int x, int y, Vector2 v)
	{
		if (isPointValid (x, y)) {
			vAve [x, y] += v * rho [x, y];
		}
	}

	void computeAverageVelocityField ()
	{
		for (int n = 0; n < N; n++) {
			for (int m = 0; m < M; m++) {
				if (rho [n, m] != 0) {
					vAve [n, m] /= rho [n, m];
				}
			}
		}
	}

	void applyPredictiveDiscomfort (float numSec, CC_Unit cc_u)
	{
		Vector2 newLoc;
		float sc;

		Vector2 xprime = cc_u.getLocalPosition () + cc_u.getVelocity () * numSec;
		float vfMag = Vector2.Distance (cc_u.getLocalPosition (), xprime);

		for (int i = 1; i < vfMag; i++) {
			newLoc = Vector2.MoveTowards (cc_u.getLocalPosition (), xprime, i);
			sc = (vfMag - i) / vfMag;				// inverse scale
			linear1stOrderSplat (newLoc, gP, sc*gP_weight);
		}
	}

	void computeSpeedField ()
	{
		for (int n = 0; n < N; n++) {
			for (int m = 0; m < M; m++) {
				for (int d = 0; d < DIR_ENWS.Length; d++) {
					f[n,m][d] = computeSpeedFieldPoint(n,m,DIR_ENWS[d]);
				}
			}
		} 
	}

	float computeSpeedFieldPoint(int x, int y, Vector2 direction) {
		int xInto = x+(int)direction.x;
		int yInto = y+(int)direction.y;

		// if we're looking in an invalid direction, dont store this value
		if (!isPointValid(xInto,yInto)) {return f_speedMin;}

		// otherwise, run the speed field calculation
		float ff=0, ft=0, fv=0;
		float r = rho[xInto,yInto];				

		// test the density INTO WHICH we move: 
		if (r < f_rhoMin) {				// rho < rho_min calc
			ft = computeTopographicalSpeed(x, y, direction);
			ff = ft;
		} else if (r > f_rhoMax) {		// rho > rho_max calc
			fv = computeFlowSpeed(xInto, yInto, direction);
			ff = fv;
		} else {						// rho in-between calc
			fv = computeFlowSpeed(xInto, yInto, direction);
			ft = computeTopographicalSpeed(x, y, direction) ;
			ff = ft + (r-f_rhoMin) / (f_rhoMax-f_rhoMin) * (fv-ft);
		}

		return Mathf.Max(f_speedMin, ff);
	}


	float computeTopographicalSpeed(int x, int y, Vector2 direction) {
		// first, calculate the gradient in the direction we are looking. By taking the dot with Direction,
		// we extract the direction we're looking and assign it a proper sign
		// i.e. if we look left (x=-1) we want -dhdx(x,y), because the gradient is assigned with a positive x
		// 		therefore:		also, Vector.left = [-1,0]
		//						Vector2.Dot(Vector.left, dh[x,y]) = -dhdx;
		float hGradientInDirection = Vector2.Dot(direction, theMap.dh[x,y]) ;
		// calculate the speed field from the equation
		return (f_speedMax + (hGradientInDirection - f_slopeMin) / (f_slopeMax - f_slopeMin) * (f_speedMin - f_speedMax) );
	}

	float computeFlowSpeed(int xI, int yI, Vector2 direction) {
		// the flow speed is simply the average velocity field of the region INTO WHICH we are looking,
		// dotted with the direction vector
		return Mathf.Max(f_speedMin, Vector2.Dot(vAve[xI,yI],direction));
	}

	void computeCostField() {
		for (int n = 0; n < N; n++) {
			for (int m = 0; m < M; m++) {
				for (int d = 0; d < DIR_ENWS.Length; d++) {
					C[n,m][d] = computeCostFieldValue(n,m,d,DIR_ENWS[d]);
				}
			}
		} 
	}

	float computeCostFieldValue(int x, int y, int d, Vector2 direction) {
		int xInto = x+(int)direction.x;
		int yInto = y+(int)direction.y;

		// if we're looking in an invalid direction, dont store this value
		if (!isPointValid(xInto,yInto) || (f[x,y][d]==0)) {return Mathf.Infinity;}

		return (f[x,y][d] * C_alpha + C_beta + gP[xInto,yInto] * C_gamma) / f[x,y][d];
	}

	void computePotentialField(Rect solve, CC_Unit_Goal_Group ccugg) {
		EikonalSolver(solve,ccugg.goal);
	}
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
	void EikonalSolver (Rect solutionSpace, Rect goalRect) {

		considered = new FastPriorityQueue <fastLocation> (N*M);

		Vector2 goalPos = goalRect.position;
		Vector2 goalDim = goalRect.size;

		Vector2 solutionPos = solutionSpace.position;
		Vector2 solutionDim = solutionSpace.size;

		Nloc = (int)solutionPos.x;
		Mloc = (int)solutionPos.y;

		Ndim = (int)solutionDim.x;
		Mdim = (int)solutionDim.y;

		accepted = new bool[Ndim,Mdim];
		goal = new bool[Ndim,Mdim];

		// start by assigning all values of potential a huge number to in-effect label them 'far'
		for (int n=Nloc; n<(Nloc+Ndim); n++) {
			for (int m=Mloc; m<(Mloc+Mdim); m++) {
				Phi[n,m] = Mathf.Infinity;
			}
		}

		// initiate by setting potential to 0 in the goal, and adding all goal locations to the considered list
		// goal locations will naturally become accepted nodes, as their 0-cost gives them 1st priority, and this
		// way, they are quickly added to accepted, while simultaneously propagating their lists and beginning the 
		// algorithm loop
		fastLocation loc;

		for (int n=(int)goalPos.x; n<(int)(goalPos.x+goalDim.x); n++) {
			for (int m=(int)goalPos.y; m<(int)(goalPos.y+goalDim.y); m++) {
				Phi[n,m] = 0f;
				loc = new fastLocation(n,m);

				markGoal(loc);

				// only consider the outer-edge of the goal... everything in the center
				// is irrelevant
				if ((n==(int)goalPos.x) || 
					(n==(int)(goalPos.x+goalDim.x)-1) || 
					(m==(int)goalPos.y) || 
					(m==(int)(goalPos.y+goalDim.y)-1)) 
				{considered.Enqueue(loc, 0f);}
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
		if ((l.x < Nloc) || (l.y < Mloc) || (l.x > (Nloc + Ndim) - 1) || (l.y > (Mloc + Mdim) - 1)) {
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
		for (int i=Nloc; i<(Ndim+Nloc); i++) {
			for (int k=Mloc; k<(Mdim+Mloc); k++) {
				if ((i!=Nloc) && (i!=(Ndim+Nloc)-1) && (k!=Mloc) && (k!=(Mdim+Mloc)-1)) 
				{writeNormalizedPotentialGradientFieldData(i,k,i-1,i+1,k-1,k+1);} // generic spot
				else if ((i==Nloc) && (k==(Mdim+Mloc)-1)) 		{writeNormalizedPotentialGradientFieldData(i,k,i,i+1,k-1,k);} 	// upper left corner
				else if ((i==(Ndim+Nloc)-1) && (k==Mloc)) 		{writeNormalizedPotentialGradientFieldData(i,k,i-1,i,k,k+1);}	// bottom left corner
				else if ((i==Nloc) && (k==Mloc)) 				{writeNormalizedPotentialGradientFieldData(i,k,i,i+1,k,k+1);}	// upper left corner
				else if ((i==(Ndim+Nloc)-1) && (k==(Mdim+Mloc)-1)) 
				{writeNormalizedPotentialGradientFieldData(i,k,i-1,i,k-1,k);} 	// bottom right corner
				else if (i==Nloc) 								{writeNormalizedPotentialGradientFieldData(i,k,i,i+1,k-1,k+1);}	// top edge
				else if (i==(Ndim+Nloc)-1) 						{writeNormalizedPotentialGradientFieldData(i,k,i-1,i,k-1,k+1);}	// bot edge
				else if (k==Mloc) 								{writeNormalizedPotentialGradientFieldData(i,k,i-1,i+1,k,k+1);}	// left edge
				else if (k==(Mdim+Mloc)-1) 						{writeNormalizedPotentialGradientFieldData(i,k,i-1,i+1,k-1,k);}	// right edge								
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

		for (int i=Nloc; i<(Ndim+Nloc); i++) {
			for (int k=Mloc; k<(Mdim+Mloc); k++) {

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

				v[i,k] = new Vector2(vx,vy);
			}
		}
	}

	// ******************************************************************************
	//			TOOLS AND UTILITIES
	//******************************************************************************
	void linear1stOrderSplat (Vector2 v, float[,] mat, float scalar)
	{
		linear1stOrderSplat (v.x, v.y, mat, scalar);
	}

	void linear1stOrderSplat (float x, float y, float[,] mat, float scalar)
	{
		int xInd = (int)Mathf.Floor (x);
		int yInd = (int)Mathf.Floor (y);

		float delx = x - xInd;
		float dely = y - yInd;

		// use += to stack density field up
		if (isPointValid (xInd, yInd)) 			{mat [xInd, yInd] += Mathf.Min (1 - delx, 1 - dely) * scalar;}
		if (isPointValid (xInd + 1, yInd)) 		{mat [xInd + 1, yInd] += Mathf.Min (delx, 1 - dely) * scalar;}
		if (isPointValid (xInd, yInd + 1)) 		{mat [xInd, yInd + 1] += Mathf.Min (1 - delx, dely) * scalar;}
		if (isPointValid (xInd + 1, yInd + 1)) 	{mat [xInd + 1, yInd + 1] += Mathf.Min (delx, dely) * scalar;}
	}

	void markAccepted(fastLocation l) {
		accepted[l.x-Nloc,l.y-Mloc] = true;
	}
	bool isLocationAccepted(fastLocation l) {
		return accepted[l.x-Nloc,l.y-Mloc];
	}
	void markGoal(fastLocation l) {
		goal[l.x-Nloc,l.y-Mloc] = true;
	}
	bool isLocationInGoal(fastLocation l) {
		return goal[l.x-Nloc,l.y-Mloc];
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
		if (theMap.g[x,y]==1) {return false;}

		return true;
	}
}