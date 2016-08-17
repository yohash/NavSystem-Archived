using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NavSystem : MonoBehaviour
{
	// single-reference NavSystem	
	public static NavSystem S;

	public bool ____mapData____;
	// mapAnalyzer scans the ground and produces the height map
	// and discomfort map that drive the NavSystem
	// EVENTUALLY REPLACE WITH LOADING OF LEVEL-DATA
	public mapAnalyzer theMapAnalyzer;
	// Tell the MapAnalyzer information about the terrain
	public int mapWidthX; 
	public int mapLengthZ;
	public float terrainMaxWorldHeight;
	public float terrainMaxHeightDifferential;
	// struct written to transfer data from MapAnalyzer to NavSystem
	public Map_Data_Package theMapData;

	public bool ____AStarPathfinding____;
	// the AStarGrid drives all our map-scale pathfinding
	public AStarGrid theAStarGrid;
	// decreasing order size of AStar nodes
	public int[] nodeDimensions;

	public bool ____ContinuumCrowds____;
	// the CCDynamicGlobalFields track tiles of moving units
	// and provide the fields for the continuum crowds solution
	public CCDynamicGlobalFields theCCDynamicFieldManager;
	public float CCTiles_UpdateFPS = 1f;
	public float CCTiles_UpdateTime;
	public int tileSize;



	public int getMapWidthX() {
		return mapWidthX;
	}
	public int getMapLengthZ() {
		return mapLengthZ;
	}


	// 00000000000000000000000000000
	// 00000000000000000000000000000
	// bullshit -- remove this later
	public GameObject MESHGEN_STAPLE;
	public GameObject TILEMAP_STAPLE;
	GameObject newTileMap;
	// ****************************************************************************************************
	//		   	INITIATION
	// ****************************************************************************************************
	void Awake ()
	{	// initialize the singleton
		S = this;
	}

	public void Initialize_NavSystem ()
	{
		// first thing is to initiate the mapAnalyzer and retrieve our map data
		// The BIG gap being left here is - LOADING MAPS AND MAP DATA
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
		theCCDynamicFieldManager = new CCDynamicGlobalFields();
		theCCDynamicFieldManager.setTileSize (tileSize);
		if (!theCCDynamicFieldManager.initiateTiles (mapWidthX, mapLengthZ, theMapData.getDiscomfortMap (), theMapData.getHeightGradientMap ())) {
			Debug.Log ("CRITICAL ERROR -- tileSize-MapDimensions MISMATCH");
		}
		CCTiles_UpdateTime = 1f / CCTiles_UpdateFPS;
		theCCDynamicFieldManager.updateTiles ();
		StartCoroutine ("updateCCTiles");
	}

	// the CCTiles are updated according to CCTiles_UpdateTime
	// this could be modified to update more slowly or quickly depending on 
	// game state
	IEnumerator updateCCTiles() {
		while (true) {
			theCCDynamicFieldManager.updateTiles ();
			yield return new WaitForSeconds (CCTiles_UpdateTime);
		}
	}

	// ****************************************************************************************************
	//		PUBLIC HANDLER FUNCTIONS
	// ****************************************************************************************************
	// 		REQUEST a ContinuumCrowds velocity field solution for a given region
	public Vector2[,] computeCCVelocityField(Rect solutionSpace, List<Location> theGoal) {
		CC_Map_Package tempMap = theCCDynamicFieldManager.buildCCMapPackage (solutionSpace);
		CCEikonalSolver cce = new CCEikonalSolver (tempMap, theGoal);
		return (cce.v);
	}

	public CCEikonalSolver DEBUG_EIKONAL_computeCCVelocityField(Rect solutionSpace, List<Location> theGoal) {
		CC_Map_Package tempMap = theCCDynamicFieldManager.buildCCMapPackage (solutionSpace);
		CCEikonalSolver cce = new CCEikonalSolver (tempMap, theGoal);
		return (cce);
	}

	// ************************************
	// 		REQUEST an optimal path from one region to another
	// 			Uses the grid produced earlier
	public List<Vector3> plotAStarOptimalPath (Vector3 start, Vector3 goal) {
		// call AStarSearch with the grid produced earlier
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

	// *********************************
	// 		CHANGE the discomfort field
	//			initial functionality will focus on obstructors, like buildings
	public void modifyDiscomfortField(int globalX, int globalY, float[,] gm) {
		// overwrite our intial Map_Data_Package
		theMapData.overwriteDiscomfortData (globalX, globalY, gm);
		// overwrite g on the tiles themselves 
		theCCDynamicFieldManager.overwriteDiscomfortDataOnTiles(globalX, globalY, gm);
		// *********** (g IS STORED IN 2 PLACES... HOW CAN i ELIMINATE THIS?) ***********************************
		// since the absolute discomfort grid g denotes unpassable regions, we now
		// have to regenerate our AStarGrid
		theAStarGrid = new AStarGrid (theMapData.getHeightMap (), theMapData.getDiscomfortMap (), nodeDimensions);
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

	public void _VISUAL_DEBUG_plotTileFields(float[,] map) {
		PLOTTING_TILE_FIELDS = true;

		newTileMap = Instantiate (TILEMAP_STAPLE) as GameObject;
		newTileMap.GetComponent<TileMap> ().BuildMesh (theMapData.getHeightMap ());

		hmap = new Texture2D (map.GetLength (0), map.GetLength (1));

		map = normalizeMatrix (map);

		for (int n = 0; n < map.GetLength (0); n++) {
			for (int m = 0; m < map.GetLength (1); m++) {
				Color c = new Color (Mathf.Abs (map [n, m]), 0f, 0f, 0.5f);
				hmap.SetPixel (n, m, c);
			}
		}
		hmap.Apply ();
		hmap.filterMode = FilterMode.Point;

		newTileMap.GetComponent<TileMap> ().BuildTexture (hmap);
	}

	public void _VISUAL_DEBUG_plotNodeCenterPoints ()
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

	public void _VISUAL_DEBUG_boxNodes ()
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



	public void _VISUAL_DEBUG_plotNodeNeighbors ()
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


	// ******************************************************************************************
	// 							HELPeR FUNCTIONS
	// ******************************************************************************************

	Color rainbow (float f)
	{
		float r, g, b;
		r = Mathf.Abs (Mathf.Sin (Mathf.Deg2Rad * (f * 360f)));
		g = Mathf.Abs (Mathf.Sin (Mathf.Deg2Rad * ((f + 0.333f) * 360f)));
		b = Mathf.Abs (Mathf.Sin (Mathf.Deg2Rad * ((f + 0.666f) * 360f)));
		return new Color (r, g, b);
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
