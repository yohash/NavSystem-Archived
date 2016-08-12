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

	public Map_Data_Package (float[,] h, float[,] g, Vector2[,] dh)
	{
		this._h = h;
		this._g = g;
		this._dh = dh;

		this._xdim = _h.GetLength (0);
		this._ydim = _h.GetLength (1);
	}

	public void overwriteDiscomfortData (Rect r)
	{
		// put code in here to change the global discomfort data
		// intended for addition of buildings
		// long term... can handle pathing with ground deformation?
	}

	// these getters containing an argument will return
	// the data within the Rect, r
	public float[,] getHeightMap (Rect r)
	{
		r = getRectContaining (r);
		float[,] ht = new float[(int)r.width, (int)r.height];
		int xt = (int)r.x;
		int yt = (int)r.y;
		for (int n = 0; n < (int)r.width; n++) {
			for (int m = 0; m < (int)r.height; m++) {
				if (pointIsValid (new Vector2 (xt + n, yt + m))) {
					ht [n, m] = _h [xt + n, yt + m];
				} else {
					ht [n, m] = 0f;
				}
			}
		}
		return ht;
	}

	public float[,] getDiscomfortMap (Rect r)
	{
		r = getRectContaining (r);
		float[,] gt = new float[(int)r.width, (int)r.height];
		int xt = (int)r.x;
		int yt = (int)r.y;
		for (int n = 0; n < (int)r.width; n++) {
			for (int m = 0; m < (int)r.height; m++) {
				if (pointIsValid (new Vector2 (xt + n, yt + m))) {
					gt [n, m] = _g [xt + n, yt + m];
				} else {
					gt [n, m] = 0f;
				}
			}
		}
		return gt;
	}

	public Vector2[,] getHeightGradientMap (Rect r)
	{
		r = getRectContaining (r);
		Vector2[,] dht = new Vector2[(int)r.width, (int)r.height];
		int xt = (int)r.x;
		int yt = (int)r.y;
		for (int n = 0; n < (int)r.width; n++) {
			for (int m = 0; m < (int)r.height; m++) {
				if (pointIsValid (new Vector2 (xt + n, yt + m))) {
					dht [n, m] = _dh [xt + n, yt + m];
				}
			}
		}
		return dht;
	}

	// default return value returns the entire map
	public float[,] getHeightMap ()
	{
		return _h;
	}

	public float[,] getDiscomfortMap ()
	{
		return _g;
	}

	public Vector2[,] getHeightGradientMap ()
	{
		return _dh;
	}

	// error-checking function to make point isnt out of bounds
	private bool pointIsValid (Vector2 p)
	{
		if ((p.x < 0) || (p.y < 0) || (p.x > _xdim - 1) || (p.y > _ydim - 1)) {
			return false;
		}
		return true;
	}

	// Rounding function to make sure the data returned is in a box
	// surrounding the sent (floating pt) coordinates.
	// Takes floor of bottom corner, and ceil of top corner
	// to encompass the sent rect
	private Rect getRectContaining (Rect r)
	{
		int x, y, dx, dy;

		x = Mathf.FloorToInt (r.x);
		y = Mathf.FloorToInt (r.y);

		dx = Mathf.CeilToInt (r.x + r.width);
		dy = Mathf.CeilToInt (r.y + r.height);

		return new Rect (x, y, dx, dy);
	}
}
