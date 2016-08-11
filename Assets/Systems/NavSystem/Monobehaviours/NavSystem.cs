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


	Map_Data_Package theMapData;

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
	}
}
