using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NavSystem : MonoBehaviour
{
	
	public static NavSystem S;
	// single-reference NavSystem

	public bool ____subSystems____;

	public mapAnalyzer theMapAnalyzer;

	public bool ____mapData____;

	public int mapWidthX, mapLengthZ;
	public float terrainMaxWorldHeight;
	public float terrainMaxHeightDifferential;

	public Map_Data_Package theMapData;

	public GameObject STAPLEMESHGEN;


	public int[] nodeDimensions;
	// decreasing order size of AStar nodes

	public AStarGrid theAStarGrid;


	public Vector2[,] get_dh ()
	{
		return theMapData.getHeightGradientMap ();
	}

	public float[,] get_h ()
	{
		return theMapData.getHeightMap ();
	}

	public float[,] get_g ()
	{
		return theMapData.getDiscomfortMap ();
	}


	void Awake ()
	{
		S = this;

		theMapAnalyzer = GetComponentInChildren<mapAnalyzer> ();
	}

	void Start ()
	{
		// first thing is to initiate the mapAnalyzer and retrieve our map data
		// The BIG gap being left here is 
		// 		- LOADING MAPS AND MAP DATA
		// calling MapAnalyzer is a temp fix
		theMapAnalyzer.setMapParameters (
			mapWidthX,
			mapLengthZ,
			terrainMaxWorldHeight,
			terrainMaxHeightDifferential
		);
		theMapData = theMapAnalyzer.collectMapData ();

		// next step is to send the Map Data to the A* "grid-ifier"
		// 		(1) scan the discomfort (g) map and build connecting series of boxes around it
		//		(2) check all boxes for neighboring boxes and create a list of node-neighbor-costs
		theAStarGrid = new AStarGrid (theMapData.getHeightMap (), theMapData.getDiscomfortMap (), nodeDimensions);



		// these are visual components for debugging
		plotNodeCenterPoints ();
		boxNodes ();
		plotNodeNeighbors ();
	}

	// ****************************************************************************************************
	//			PUBLIC HANDLER FUNCTIONS
	// ****************************************************************************************************

	public List<Vector3> plotAStarOptimalPath (Vector3 start, Vector3 goal) {
		AStarSearch astar = new AStarSearch (theAStarGrid, start, goal);

		List<AStarNode> path = constructOptimalPath(astar, astar.start, astar.goal);
		List<Vector3> pathLocations = new List<Vector3>();
		Vector3 pathData;

		pathLocations.Add (new Vector3 (goal.x, theMapData.getHeightMap () [(int)goal.x,(int) goal.y], goal.y));
		foreach (AStarNode l in path) {
			pathData = new Vector3 (l.x, theMapData.getHeightMap () [(int)l.x, (int) l.y], l.y);
			pathLocations.Add(pathData);
		}
		pathLocations.Add (new Vector3 (start.x, theMapData.getHeightMap () [(int)start.x,(int) start.y], start.y));

		return pathLocations;
	}


	List<AStarNode> constructOptimalPath(AStarSearch astar, AStarNode theStart, AStarNode theGoal) {
		List<AStarNode> newPath = new List<AStarNode>();
		AStarNode current = theGoal;

		newPath.Add(theGoal);

		while(current != theStart) {
			current = astar.cameFrom[current];
			Debug.Log ("came from: ("+current.x+","+current.y+")");
			newPath.Add(current);
		}
		return newPath;
	}


	// ****************************************************************************************************
	//			VISUALIZATION FUNCTIONS
	// ****************************************************************************************************
	void plotNodeCenterPoints ()
	{
		float highest = Mathf.Max (nodeDimensions);
		float lowest = Mathf.Min (nodeDimensions);

		for (int k = 0; k < theAStarGrid.nodes.Count; k++) {
			float val = (((float)theAStarGrid.nodes [k].getWidth() - lowest) / (highest - lowest));

			float x = theAStarGrid.nodes [k].x ;
			float y = theAStarGrid.nodes [k].y ;

			Debug.DrawRay (new Vector3 (x, 10f, y), Vector3.down * 10f, rainbow (val), 10f);
		}
	}

	void boxNodes ()
	{
		Vector3[] points;
		Vector3[] norms = new Vector3[]{ Vector3.up, Vector3.up, Vector3.up, Vector3.up, Vector3.up };
		GameObject go;

		float highest = Mathf.Max (nodeDimensions);
		float lowest = Mathf.Min (nodeDimensions);

		for (int k = 0; k < theAStarGrid.nodes.Count; k++) {
			int x = (theAStarGrid.nodes [k].getXCorner());
			int y = (theAStarGrid.nodes [k].getYCorner());
			int h = (theAStarGrid.nodes [k].getHeight());
			int w = (theAStarGrid.nodes [k].getWidth());

			float val = (((float)w - lowest) / (highest - lowest));

			go = Instantiate (STAPLEMESHGEN) as GameObject;
			Vector3 pt1 = new Vector3 (x, theMapData.getHeightMap () [x, y], y);
			Vector3 pt2 = new Vector3 (x + w + 1, theMapData.getHeightMap () [x + w, y], y);
			Vector3 pt3 = new Vector3 (x + w + 1, theMapData.getHeightMap () [x + w, y + h], y + h + 1);
			Vector3 pt4 = new Vector3 (x, theMapData.getHeightMap () [x, y + h], y + h + 1);
			Vector3 pt5 = new Vector3 (x, theMapData.getHeightMap () [x, y], y);

			points = new Vector3[]{ pt1, pt2, pt3, pt4, pt5 };

			meshLineGenerator m = go.GetComponent<meshLineGenerator> ();

			m.setLinePoints (points, norms);
			m.generateMesh ();
			m.setColor (rainbow (val));
		}
	}

	Color rainbow (float f)
	{
		float r, g, b;
		r = Mathf.Abs (Mathf.Sin (Mathf.Deg2Rad * (f * 360f)));
		g = Mathf.Abs (Mathf.Sin (Mathf.Deg2Rad * ((f + 0.333f) * 360f)));
		b = Mathf.Abs (Mathf.Sin (Mathf.Deg2Rad * ((f + 0.666f) * 360f)));
		return new Color (r, g, b);
	}


	void plotNodeNeighbors ()
	{
		List<AStarNode> nodes = theAStarGrid.nodes;

		for (int i = 0; i < nodes.Count; i++) {
			AStarNode an1 = nodes [i];

			float thisY = Random.value + 8f;
			Color c = new Color (Random.value, Random.value, Random.value);

			List<AStarNeighbor> theNeibs = theAStarGrid.nodeNeighbors [an1];

			for (int k = 0; k < theNeibs.Count; k++) {
				AStarNeighbor an2 = theNeibs [k];

				float xs = an1.x ;
				float ys = an1.y ;

				float xf = an2.theNode.x ;
				float yf = an2.theNode.y ;

				Vector3 start = new Vector3 (xs, thisY, ys);
				Vector3 fin = new Vector3 (xf, thisY, yf);
				Vector3 dir = fin - start;

				Debug.DrawRay (start, dir, c, 10f);
			}
		}
	}
}
