using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// the Map_Data_Package is meant to synergize with MapAnalyzer
// and NavSystem

// CURRENTLY, MapAnalyzer ASSUMES:
//			- a 1-m scan grid!!!!
//			- map lower corner at (x,z) = (0,0)

// this means, when calling a block (rect) of data, you can
// enter the real world coordinates, and the data will be taken from
// a block containing those coordinates

public struct Map_Data_Package
{
	private float[,] _h, _g;
	private Vector2[,] _dh;

	private int _xdim, _ydim;

	public Map_Data_Package (float[,] h, float [,] g, Vector2[,] dh)
	{
		this._h = h;
		this._g = g;
		this._dh = dh;

		this._xdim = _h.GetLength (0);
		this._ydim = _h.GetLength (1);
	}

	public void overwriteDiscomfortData(Rect r) {
		// put code in here to change the global discomfort data
		// intended for addition of buildings
		// long term... can handle pathing with ground deformation?
	}

	// 
	public float[,] getHeightMap(Rect r) {
		float[,] h = new float[(int) r.width, (int) r.height];

		return _h;
	}
	public float[,] getDiscomfortMap(Rect r) {
		return _g;
	}
	public Vector2[,] getHeightGradientMap(Rect r) {
		return _dh;
	}

	// default return value returns the entire map
	public float[,] getHeightMap() {
		return _h;
	}
	public float[,] getDiscomfortMap() {
		return _g;
	}
	public Vector2[,] getHeightGradientMap() {
		return _dh;
	}

	// error-checking function to make point isnt out of bounds
	private bool isPointValid(Vector2 p) {
		if ((p.x < 0) || (p.y < 0) || (p.x > _xdim - 1) || (p.y > _ydim - 1)) {
			return false;
		}
		return true;
	}

	// rounding function to make sure the data returned 
	private Rect getRectContaining(Rect r) {
		int x, y, dx, dy;

		x = Mathf.FloorToInt (r.x);
		y = Mathf.FloorToInt (r.y);

		dx = Mathf.CeilToInt (r.x + r.width);
		dy = Mathf.CeilToInt (r.y + r.height);

		return new Rect (x, y, dx, dy);
	}
}
