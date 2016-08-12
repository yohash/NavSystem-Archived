using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class NavSystem : MonoBehaviour {
	
	public static NavSystem S;			// single-reference NavSystem

	public bool ____subSystems____;

	public mapAnalyzer theMapAnalyzer;

	public bool ____mapData____;

	public int mapWidthX, mapLengthZ;
	public float terrainMaxWorldHeight;
	public float terrainMaxHeightDifferential;

	public Map_Data_Package theMapData;

	public GameObject STAPLEMESHGEN;
	List<meshLineGenerator> MESHLINEGENs;


	public int[] nodeHalfDimensions;			// decreasing order size of AStar nodes

	public AStarGrid theAStarGrid;

	public Vector2[,] get_dh() {return theMapData.getHeightGradientMap();}
	public float[,] get_h() {return theMapData.getHeightMap();}
	public float[,] get_g() {return theMapData.getDiscomfortMap();}


	void Awake () {
		S = this;

		theMapAnalyzer = GetComponentInChildren<mapAnalyzer> ();
	}

	void Start () {
		// first thing is to initiate the mapAnalyzer and retrieve our map data
		// The BIG gap being left here is 
		// 		- LOADING MAPS AND MAP DATA
		// calling MapAnalyzer is a temp fix
		theMapAnalyzer.setMapParameters(
			mapWidthX,
			mapLengthZ,
			terrainMaxWorldHeight,
			terrainMaxHeightDifferential
		);

		theMapData = theMapAnalyzer.collectMapData();

		// next step is to send the Map Data to the A* "grid-ifier"
		// this will 	(1) scan the discomfort (g) map and build connecting
		//					series of boxes around it
		//				(2) check all boxes for neighboring boxes and create
		//					a list of node-neighbor-costs

		// approach:  build an A*-grid class??
		//		- then we can create an instance of it here and initiate
		//		- if we need to rebuild the mesh, we can ask the class to 
		//		  remake it (ie. when buildings are built)

		theAStarGrid = new AStarGrid (theMapData.getHeightMap(), theMapData.getDiscomfortMap(), nodeHalfDimensions);


		fuckWithNodes ();
	}

	void fuckWithNodes() {
		foreach (AStarNode an in theAStarGrid.nodes) {
			if (an.dim == nodeHalfDimensions [0]) {
				Debug.DrawRay (new Vector3 (an.x, 10f, an.y), Vector3.down * 10f, Color.black, 10f);
			} else if (an.dim == nodeHalfDimensions [1]) {
				Debug.DrawRay (new Vector3 (an.x, 10f, an.y), Vector3.down * 10f, Color.blue, 10f);
			} else if (an.dim == nodeHalfDimensions [2]) {
				Debug.DrawRay (new Vector3 (an.x, 10f, an.y), Vector3.down * 10f, Color.cyan, 10f);
			} else if (an.dim == nodeHalfDimensions [3]) {
				Debug.DrawRay (new Vector3 (an.x, 10f, an.y), Vector3.down * 10f, Color.green, 10f);
			} else if (an.dim == nodeHalfDimensions [4]) {
				Debug.DrawRay (new Vector3 (an.x, 10f, an.y), Vector3.down * 10f, Color.yellow, 10f);
			}
		}
	}
}
