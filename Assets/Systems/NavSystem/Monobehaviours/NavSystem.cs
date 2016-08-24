using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Threading;
using Foundation.Tasks;

public class NavSystem : MonoBehaviour
{
	public bool DEBUG_VIEW_TILE = false;

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

	public bool ____AStarPathfinding____;
	// the AStarGrid drives all our map-scale pathfinding
	public AStarGrid theAStarGrid;
	// cache the search for multithreading
	AStarSearch astar;
	// decreasing order size of AStar nodes
	public int[] nodeDimensions;

	public bool ____ContinuumCrowds____;
	// the CCDynamicGlobalFields track tiles of moving units
	// and provide the fields for the continuum crowds solution
	public CCDynamicGlobalFields theCCDynamicFieldManager;
	public float CCTiles_UpdateFPS;
	public float CCTiles_UpdateTime;
	public int tileSize;
	// cache two 'heavier' classes
	CC_Map_Package tempMap;
	CCEikonalSolver cce;

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
	TileMap tm;
	// 00000000000000000000000000000
	// ****************************************************************************************************
	//		   	INITIATION
	// ****************************************************************************************************
	void Awake ()
	{	// initialize the singleton
		S = this;
	}

	public void Initialize_NavSystem ()
	{
		// 0000000000000000000000000000000000000000000000000000000000
		// 0000000000000000000000000000000000000000000000000000000000
		// bullshit -- remove this later
		newTileMap = Instantiate (TILEMAP_STAPLE) as GameObject;
		tm = newTileMap.GetComponent<TileMap> ();
		// 0000000000000000000000000000000000000000000000000000000000

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
		Map_Data_Package theMapData = theMapAnalyzer.collectMapData ();

		// next step is to send the Map Data to the A* "grid-ifier"
		// 		(1) scan the discomfort (g) map and build connecting series of boxes around it
		//		(2) check all boxes for neighboring boxes and create a list of node-neighbor-costs
		theAStarGrid = new AStarGrid (theMapData.getCompleteHeightMap (), theMapData.getCompleteDiscomfortMap (), nodeDimensions);

		// next, we need to initiate the Continuum Crowds Dynamic Global Tile manager to
		// instantiate all its tiles, and fill them with their core data
		theCCDynamicFieldManager = new CCDynamicGlobalFields();
		theCCDynamicFieldManager.setMapData (theMapData);
		theCCDynamicFieldManager.setTileSize (tileSize);
		theCCDynamicFieldManager.initiateTiles ();

		// next, we start the CC Dynamic Global Fields Tile manager
		// to begin updating its fields
		CCTiles_UpdateTime = 1f / CCTiles_UpdateFPS;
		theCCDynamicFieldManager.updateTiles ();
		StartCoroutine ("updateCCTiles");
	}

	// the CCTiles are updated according to CCTiles_UpdateTime
	// this could be modified to update more slowly or quickly depending on 
	// game state
	IEnumerator updateCCTiles() {
		while (true) {
			if (DEBUG_VIEW_TILE) {
				theCCDynamicFieldManager.drawRhoOnTile (new Location (0, 0));
			}
			// first, update the positions/velocities of all CC_Units
			theCCDynamicFieldManager.updateCCUnits();
			// now, have the CCDynamicGlobalFieldManager updateTiles() in another thread
			StartCoroutine("_MultiThread_CCDyn_UpdateTiles");
			yield return new WaitForSeconds (CCTiles_UpdateTime * Random.Range(0.8f, 1.2f));
		}
	}
	// ****************************************************************************************************
	// ****************************************************************************************************
	//		MULTI-THREADED FUNCTIONS, THANKS TO UNITY TASKS
	// ****************************************************************************************************
	IEnumerator _MultiThread_CCDyn_UpdateTiles() {
		var task = UnityTask.Run (() => {
			theCCDynamicFieldManager.updateTiles ();
		});
		yield return task; 
	}

	// ****************************************************************************************************
	// ****************************************************************************************************
	//		PUBLIC HANDLER FUNCTIONS
	// ****************************************************************************************************
	public float getHeightAtPoint(float x, float y) {
		return theCCDynamicFieldManager.theMapData.getInterpHeightMap (x, y);
	}

	public AStarGrid getAStarGrid() {
		return theAStarGrid;
	}

	public CC_Map_Package getCCMapPackageFromRect(Rect solutionSpace) {
		return theCCDynamicFieldManager.buildCCMapPackage (solutionSpace);
	}

	public List<Location> getGoalAsLocations(Rect solutionSpace, List<Rect> theGoal) {
		return convertRectsToLocations(solutionSpace, theGoal);
	}

	public void addCCUnitToDynamicFields(Unit u) {
		theCCDynamicFieldManager.addNewCCUnit (convertUnit_CCUnit (u));
	}

	// 	CHANGE the discomfort field - initial functionality will focus on obstructors, like buildings
	public void modifyDiscomfortField(int globalX, int globalY, float[,] gm) {
		// overwrite our Map_Data_Package
		theCCDynamicFieldManager.theMapData.overwriteDiscomfortData (globalX, globalY, gm);
		// since the absolute discomfort grid g denotes unpassable regions
		// we now have to regenerate our AStarGrid
		theAStarGrid = new AStarGrid (theCCDynamicFieldManager.theMapData.getCompleteHeightMap (), 
			theCCDynamicFieldManager.theMapData.getCompleteDiscomfortMap (), nodeDimensions);
	}
	// ****************************************************************************************************
	// ****************************************************************************************************
	//			BACKGROUND FUNCTIONS
	// ****************************************************************************************************
	CC_Unit convertUnit_CCUnit(Unit u) {
		CC_Unit ccu = new CC_Unit (u);
		return (ccu);
	}

	// ****************************************************************************************************
	//			VISUALIZATION FUNCTIONS
	// ****************************************************************************************************
	public Texture2D hmap;

	public void _DEBUG_VISUAL_plotTileFields(Vector2 corner, float[,] map) {

		Rect range = new Rect(corner, new Vector2(map.GetLength (0), map.GetLength (1)));
		float[,] mappy = theCCDynamicFieldManager.theMapData.getRangeOfHeightMap (range);
		tm.BuildMesh (corner, mappy);
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

	public void _DEBUG_VISUAL_plotTileFields(Vector2 corner, Vector2[,] map) {

		Rect range = new Rect(corner, new Vector2(map.GetLength (0), map.GetLength (1)));
		float[,] mappy = theCCDynamicFieldManager.theMapData.getRangeOfHeightMap (range);
		tm.BuildMesh (corner, mappy);
		hmap = new Texture2D (map.GetLength (0), map.GetLength (1));
		float mm = vect2MatrixMax (map);

		for (int n = 0; n < map.GetLength (0); n++) {
			for (int m = 0; m < map.GetLength (1); m++) {
				Color c = new Color (Mathf.Abs (map [n, m].x)/mm, 0f, Mathf.Abs (map [n, m].y)/mm, 0.5f);
				hmap.SetPixel (n, m, c);
			}
		}
		hmap.Apply ();
		hmap.filterMode = FilterMode.Point;

		newTileMap.GetComponent<TileMap> ().BuildTexture (hmap);
	}

	public void _DEBUG_VISUAL_plotNodeCenterPoints ()
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

	public void _DEBUG_VISUAL_boxAStarNodes  ()
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
			Vector3 pt1 = new Vector3 (x, theCCDynamicFieldManager.theMapData.getHeightMap (x, y), y);
			Vector3 pt2 = new Vector3 (x + w + 1, theCCDynamicFieldManager.theMapData.getHeightMap (x + w, y), y);
			Vector3 pt3 = new Vector3 (x + w + 1, theCCDynamicFieldManager.theMapData.getHeightMap (x + w, y + h), y + h + 1);
			Vector3 pt4 = new Vector3 (x, theCCDynamicFieldManager.theMapData.getHeightMap (x, y + h), y + h + 1);
			Vector3 pt5 = new Vector3 (x, theCCDynamicFieldManager.theMapData.getHeightMap (x, y), y);

			points = new Vector3[]{ pt1, pt2, pt3, pt4, pt5 };

			meshLineGenerator m = go.GetComponent<meshLineGenerator> ();

			m.setLinePoints (points, norms);
			m.generateMesh ();
			m.setColor (rainbow (val));
		}
	}

	public void _DEBUG_VISUAL_plotNodeNeighbors ()
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

	float vect2MatrixMax(Vector2[,] f) {
		float maxX = 0f, maxY= 0f;

		for (int n=0; n<f.GetLength(0); n++) {
			for (int m=0; m<f.GetLength(1); m++) {
				if (!float.IsInfinity(f[n,m].x) && !float.IsNaN(f[n,m].x) &&  Mathf.Abs( f[n, m].x ) > maxX) {
					maxX = Mathf.Abs( f [n, m].x);
				}
				if (!float.IsInfinity(f[n,m].y) && !float.IsNaN(f[n,m].y) &&  Mathf.Abs( f [n, m].y) > maxY) {
					maxY = Mathf.Abs( f [n, m].y);
				}
			}
		}

		float mm = Mathf.Max(maxX,maxY);

		return mm;
	}

	List<Location> convertRectsToLocations(Rect anchor, List<Rect> rects) {
		List<Location> locations = new List<Location> ();
		foreach (Rect r in rects) {
			for (int x = Mathf.FloorToInt (r.x - anchor.x); x < Mathf.CeilToInt (r.x + r.width - anchor.x); x++) {
				for (int y = Mathf.FloorToInt (r.y - anchor.y); y < Mathf.CeilToInt (r.y + r.height - anchor.y); y++) {
					Location l = new Location (x, y);
					if (!locations.Contains (l)) {
						locations.Add (l);
					}
				}
			}
		}
		return locations;
	}
}
