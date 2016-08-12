using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// the MapAnalyzer is a MonoBehaviour whose purpose is to assess the map
// by using a series of raycasts, and build a Map_Data_Package to send to
// the NavSystem. Plots generated are h, dh, and g

// CURRENTLY, MapAnalyzer ASSUMES:
//			- a 1-m scan grid!!!!
//			- map lower corner at (x,z) = (0,0)

// MapAnalyzer is expected to become deprecated/obsolete once loading/saving
// of pre-built maps is initiated

public class mapAnalyzer : MonoBehaviour
{
	// ******************	textures
	public bool ___TextureMaps___;
	public Texture2D heightMap, discomfortMap;

	// ******************	mapping inputs
	private float _terrainMaxWorldHeight;
	private float _terrainMaxHeightDifferential;

	private int _mapWidthX, _mapLengthZ;
	private float stepSize = 1f;

	private float slopeMax = 0.6f;			// max slope for marking a grid spot unpathable

	// ******************	derived values
	int xSteps, zSteps;

	// *****************************************************************************************************************
	// 				PUBLIC HANDLERS
	// *****************************************************************************************************************
	public void setMapParameters (int mapWidthX, int mapLengthZ, float maxTerrainWorldHeight, float maxTerrainHeightDifferential)
	{
		// ASSUMED PARAMETERS (for now)
		//		- stepSize (basically, resolution, is fixed at 1 m)
		//		- map location - it will be assumed map is at (x,z) = (0,0)
		_mapWidthX = mapWidthX;
		_mapLengthZ = mapLengthZ;
		_terrainMaxWorldHeight = maxTerrainWorldHeight;
		_terrainMaxHeightDifferential = maxTerrainHeightDifferential;

		xSteps = (int)(_mapWidthX / stepSize);
		zSteps = (int)(_mapLengthZ / stepSize);
	}

	public void setMaxSlopeForUnpathable(float f) {
		slopeMax = f;
	}

	public Map_Data_Package collectMapData ()
	{
		float[,] h, g;
		Vector2[,] dh;

		h = new float[xSteps, zSteps];
		g = new float[xSteps, zSteps];
		dh = new Vector2[xSteps, zSteps];

		h = generateHeightMap ();
		dh = generateMatrixGradient (h);
		g = generateDiscomfortMap (dh);

		Map_Data_Package mapData = new Map_Data_Package (h, g, dh);

		discomfortMap = TextureGenerator.TextureFromMap (g);

		return mapData;
	}

	// *****************************************************************************************************************
	// 				HEIGHT MAP GENERATION
	// *****************************************************************************************************************
	private float[,] generateHeightMap ()
	{
		float[,] _h = new float[xSteps, zSteps];

		float xoffset = stepSize / 2f;
		float zoffset = stepSize / 2f;

		for (int i = 0; i < xSteps; i++) {
			for (int k = 0; k < zSteps; k++) {
				_h [i, k] = getHeightAndNormalDataForPoint (stepSize * i + xoffset, stepSize * k + zoffset) [0].y;
			}
		}

		return _h;
	}

	private Vector2[,] generateMatrixGradient (float[,] M)
	{
		Vector2[,] dM = new Vector2[xSteps, zSteps];

		for (int i = 0; i < xSteps; i++) {
			for (int k = 0; k < zSteps; k++) {							// generic spot
				if ((i != 0) && (i != xSteps - 1) && (k != 0) && (k != zSteps - 1)) {
					dM [i, k] = writeGradientData (M, i, k, i - 1, i + 1, k - 1, k + 1);
				} else if ((i == 0) && (k == zSteps - 1)) {				// upper left corner
					dM [i, k] = writeGradientData (M, i, k, i, i + 1, k - 1, k);
				} else if ((i == xSteps - 1) && (k == 0)) {				// bottom left corner
					dM [i, k] = writeGradientData (M, i, k, i - 1, i, k, k + 1);
				} else if ((i == 0) && (k == 0)) {						// upper left corner
					dM [i, k] = writeGradientData (M, i, k, i, i + 1, k, k + 1);
				} else if ((i == xSteps - 1) && (k == zSteps - 1)) {	// bottom right corner
					dM [i, k] = writeGradientData (M, i, k, i - 1, i, k - 1, k);
				} else if (i == 0) {									// top edge
					dM [i, k] = writeGradientData (M, i, k, i, i + 1, k - 1, k + 1);
				} else if (i == xSteps - 1) {							// bot edge
					dM [i, k] = writeGradientData (M, i, k, i - 1, i, k - 1, k + 1);
				} else if (k == 0) {									// left edge
					dM [i, k] = writeGradientData (M, i, k, i - 1, i + 1, k, k + 1);
				} else if (k == zSteps - 1) {							// right edge		
					dM [i, k] = writeGradientData (M, i, k, i - 1, i + 1, k - 1, k);
				}							
			}
		}

		return dM;
	}

	// this performs a center-gradient for interior points,
	// and is how MATLAB calculates gradients of matrices
	private Vector2 writeGradientData (float[,] mat, int x, int y, int xMin, int xMax, int yMin, int yMax)
	{
		Vector2 _dM = new Vector2 (
			              (mat [xMax, y] - mat [xMin, y]) / (xMax - xMin), 
			              (mat [x, yMax] - mat [x, yMin]) / (yMax - yMin)
		              );
		return _dM;
	}

	private float[,] generateDiscomfortMap (Vector2[,] dh)
	{
		float[,] _g = new float[xSteps, zSteps];

		for (int i = 0; i < xSteps; i++) {
			for (int k = 0; k < zSteps; k++) {
				if (Mathf.Max (Mathf.Abs (dh [i, k].x), Mathf.Abs (dh [i, k].y)) > slopeMax) {
					_g [i, k] = 1f;
				}
			}
		}

		return _g;
	}

	// *****************************************************************************************************************
	// 		HELPer functions
	// *****************************************************************************************************************
	Vector3 rayPoint, rayDir;

	Vector3[] getHeightAndNormalDataForPoint (float x, float z)
	{

		rayPoint = new Vector3 (x, _terrainMaxWorldHeight * 1.1f, z);
		rayDir = new Vector3 (0, -_terrainMaxHeightDifferential, 0);

		Ray ray = new Ray (rayPoint, rayDir);

		RaycastHit hit;
		int mask = 1 << 8;
		if (Physics.Raycast (ray, out hit, _terrainMaxHeightDifferential * 1.1f, mask)) {
			return new Vector3[2] { hit.point, hit.normal };
		}
		return new Vector3[2] { Vector3.zero, Vector3.zero };
	}

	float[,] normalizeMap (float[,] unNormMap)
	{
		float maxHeight = 0f;
		for (int i = 0; i < xSteps; i++) {
			for (int k = 0; k < zSteps; k++) {
				if (unNormMap [i, k] > maxHeight) {
					maxHeight = unNormMap [i, k];
				}
			}
		}
		if (maxHeight > 0) {
			for (int i = 0; i < xSteps; i++) {
				for (int k = 0; k < zSteps; k++) {
					unNormMap [i, k] /= maxHeight;
				}
			}
		}
		return unNormMap;
	}

	// *****************************************************************************************************************
	// 		ADMIN and DEBUG functions
	// *****************************************************************************************************************
	public void printOutMatrix (float[,] f)
	{
		string s;
		for (int n = 0; n < f.GetLength (0); n++) {
			s = "";
			for (int m = 0; m < f.GetLength (1); m++) {
				if (n == f.GetLength (0) - 1 && m == f.GetLength (1) - 1) {
					s += " X ";
				} else if (n == 0 && m == 0) {
					s += " X ";
				} 
				s += f [n, m].ToString () + " "; 
			}
			Debug.Log (s);
		}
	}
}