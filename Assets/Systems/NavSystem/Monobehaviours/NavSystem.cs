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


	public bool ____pathFinding____;
	// decreasing order size of AStar nodes
	public int[] nodeDimensions;

	// the AStarGrid drives all our map-scale pathfinding
	public AStarGrid theAStarGrid;

	// the CCDynamicGlobalFields track tiles of moving units
	// and provide the fields for the continuum crowds solution
	public CCDynamicGlobalFields CCDyn;
	public float CCTiles_UpdateFPS = 10f;
	public float CCTiles_UpdateTime;

	// *****************************
	// bullshit -- remove this later
	public GameObject MESHGEN_STAPLE;
	public GameObject TILEMAP_STAPLE;
	GameObject newTileMap;

	public GameObject aCCU;

	CCEikonalSolver cce;

	void Awake ()
	{
		S = this;
	}

	void Start ()
	{
		// first thing is to initiate the mapAnalyzer and retrieve our map data
		// The BIG gap being left here is 
		// 		- LOADING MAPS AND MAP DATA
		// calling MapAnalyzer is a temp fix
		theMapAnalyzer = GetComponentInChildren<mapAnalyzer> ();
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

		// next, we need to initiate the Continuum Crowds Dynamic Global Tile manager to
		// instantiate all its tiles, and fill them with their core data
		CCDyn = new CCDynamicGlobalFields();
		CCDyn.setTileSize (mapWidthX);
		CCDyn.addNewCCUnit (aCCU.GetComponent<CC_Unit>());
		if (!CCDyn.initiateTiles (mapWidthX, mapLengthZ, theMapData.getDiscomfortMap (), theMapData.getHeightGradientMap ())) {
			Debug.Log ("CRITICAL ERROR -- tileSize-MapDimensions MISMATCH");
		}
		CCTiles_UpdateTime = 1f / CCTiles_UpdateFPS;
		CCDyn.updateTiles ();
		StartCoroutine ("updateCCTiles");







		// **** these are visual components for debugging
		newTileMap = Instantiate (TILEMAP_STAPLE) as GameObject;
//		plotTileFields();
		Invoke ("performEikonalShit", 2f);
//		theMapAnalyzer.printOutMatrix (cce.Phi);
		// plotNodeCenterPoints ();
//		 boxNodes ();
		// plotNodeNeighbors ();
	}

	void performEikonalShit(){

		List<Location> goalies = new List<Location> ();
		goalies.Add (new Location (37, 42));
		cce = GIMME_DA_EIKONAL_SOLUTION (new Rect (0, 0, 50, 50), goalies); 

		printMat (normalizeMatrix( cce.Phi));

		plotTileFields ();
	}

	void printMat(float[,] v ) {
		string s;
		Debug.Log ("NEW MAT");
		for (int i = 0; i < v.GetLength (0); i++) {
			s = "";
			for (int k = 0; k < v.GetLength (0); k++) {
				s += v [i, k];
				s += " ";
			}
			Debug.Log (s);
		}
	}

	IEnumerator updateCCTiles() {
		while (true) {
			CCDyn.updateTiles ();

			if (PLOTTING_TILE_FIELDS) {
//				plotTileFields();
			}

			yield return new WaitForSeconds (CCTiles_UpdateTime);
		}
	}

	// ****************************************************************************************************
	//			PUBLIC HANDLER FUNCTIONS
	// ****************************************************************************************************

	public Vector2[,] computeCCVelocityField(Rect solutionSpace, List<Location> theGoal) {
		CC_Map_Package tempMap = CCDyn.buildCCMapPackage (solutionSpace);
		CCEikonalSolver cce = new CCEikonalSolver (tempMap, theGoal);
		return (cce.v);
	}

	public CCEikonalSolver GIMME_DA_EIKONAL_SOLUTION(Rect solutionSpace, List<Location> theGoal) {
		CC_Map_Package tempMap = CCDyn.buildCCMapPackage (solutionSpace);
		CCEikonalSolver cce = new CCEikonalSolver (tempMap, theGoal);
		return (cce);
	}

	public List<Vector3> plotAStarOptimalPath (Vector3 start, Vector3 goal) {
		AStarSearch astar = new AStarSearch (theAStarGrid, start, goal);

		List<Vector3> pathLocations = new List<Vector3> ();

		if (astar.cameFrom.ContainsKey (astar.goal)) {
			List<AStarNode> path = constructOptimalPath (astar, astar.start, astar.goal);
			Vector3 pathData;

			pathLocations.Add (new Vector3 (goal.x, theMapData.getHeightMap (goal.x, goal.y), goal.y));
			foreach (AStarNode l in path) {
				pathData = new Vector3 (l.x, theMapData.getHeightMap (l.x, l.y), l.y);
				pathLocations.Add (pathData);
			}
			pathLocations.Add (new Vector3 (start.x, theMapData.getHeightMap (start.x, start.y), start.y));
		}
		return pathLocations;
	}

	List<AStarNode> constructOptimalPath(AStarSearch astar, AStarNode theStart, AStarNode theGoal) {
		List<AStarNode> newPath = new List<AStarNode>();
		AStarNode current = theGoal;
		newPath.Add(theGoal);
		while(current != theStart) {
			current = astar.cameFrom[current];
			newPath.Add(current);
		}
		return newPath;
	}


	// ****************************************************************************************************
	//			VISUALIZATION FUNCTIONS
	// ****************************************************************************************************
	bool PLOTTING_TILE_FIELDS = false;

	public Texture2D hmap;

	void plotTileFields() {
		PLOTTING_TILE_FIELDS = true;

		newTileMap.GetComponent<TileMap> ().BuildMesh (theMapData.getHeightMap ());

		hmap = new Texture2D (mapWidthX, mapLengthZ);

		float[,] map = normalizeMatrix (cce.Phi);

		foreach (CC_Tile cct in CCDyn.getTiles()) {
			for (int n=0; n<mapWidthX; n++) {
				for (int m=0; m<mapLengthZ; m++) {
					Color c = new Color(Mathf.Abs(map[n,m]), 0f, 0f, 0.5f);
					hmap.SetPixel (n, m, c);
				}
			}
			hmap.Apply ();
			hmap.filterMode = FilterMode.Point;

			newTileMap.GetComponent<TileMap> ().BuildTexture (hmap);
		}
	}

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

			go = Instantiate (MESHGEN_STAPLE) as GameObject;
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

	float[,] normalizeMatrix(float[,] f) {
		float maxV = 0f;

		for (int n=0; n<f.GetLength(0); n++) {
			for (int m=0; m<f.GetLength(1); m++) {
				if (!float.IsInfinity(f[n,m]) && !float.IsNaN(f[n,m]) &&  f [n, m] > maxV) {
					maxV = f [n, m];
				}
			}
		}

		for (int n=0; n<f.GetLength(0); n++) {
			for (int m=0; m<f.GetLength(1); m++) {
				f [n, m] /= maxV;
			}
		}

		return f;
	}
}
