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

	public void overwriteDiscomfortData (int globalX, int globalY, float[,] gm)
	{
		// intended for addition of buildings
		for (int xI = 0; xI < (gm.GetLength(0)); xI++) {
			for (int yI = 0; yI < (gm.GetLength(1)); yI++) {
				if (pointIsValid (new Vector2(xI + globalX, yI + globalY))) {
					_g [xI + globalX, yI + globalY] = gm [xI, yI];
				}
			}
		}
		// long term... can handle pathing with ground deformation?
		// after changing what g "means", can add fluid values from 0-1 to produce 
		// dynamic unit perference for moving on particular regions
		// e.g. Battle Manager can build a large, arching path that our units will desire to follow
		//		using splines/beziers so that AStar Grid following looks smoother
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

	// return a single point
	public float getHeightMap (float x, float y) {
		return interpolateBetweenValues (x, y, _h);
	}
	public float getDiscomfortMap (float x, float y) {
		return interpolateBetweenValues (x, y, _g);
	}
	public Vector2 getHeightGradientMap (float x, float y) {
		return interpolateBetweenValues (x, y, _dh);
	}

	// return a single point
	public float getHeightMap (int x, int y) {
		return _h [x, y];
	}
	public float getDiscomfortMap (int x, int y) {
		return _g [x, y];
	}
	public Vector2 getHeightGradientMap (int x, int y) {
		return _dh [x, y];
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


	// interpolation function taken from CC files...
	// maybe should compile these things 
	float interpolateBetweenValues(float x, float y, float[,] array) {
		float xcomp;

		int xl = array.GetLength(0);
		int yl = array.GetLength(1);

		int topLeftX = (int)Mathf.Floor(x);
		int topLeftY = (int)Mathf.Floor(y);

		float xAmountRight = x - topLeftX;
		float xAmountLeft = 1.0f - xAmountRight;
		float yAmountBottom = y - topLeftY;
		float yAmountTop = 1.0f - yAmountBottom;

		Vector4 valuesX = Vector4.zero;

		if (isPointInsideArray(topLeftX,topLeftY,xl,yl))			{valuesX[0] = array[topLeftX, topLeftY];}
		if (isPointInsideArray(topLeftX + 1, topLeftY,xl,yl)) 		{valuesX[1] = array[topLeftX + 1, topLeftY];}
		if (isPointInsideArray(topLeftX, topLeftY + 1,xl,yl)) 		{valuesX[2] = array[topLeftX, topLeftY + 1];}
		if (isPointInsideArray(topLeftX + 1, topLeftY + 1,xl,yl)) 	{valuesX[3] = array[topLeftX + 1, topLeftY + 1];}
		for (int n=0; n<4; n++) {
			if (float.IsNaN(valuesX[n])) {valuesX[n] = 0f;}
			if (float.IsInfinity(valuesX[n])) {valuesX[n] = 0f;}
		}

		float averagedXTop = valuesX[0] * xAmountLeft + valuesX[1] * xAmountRight;
		float averagedXBottom = valuesX[2] * xAmountLeft + valuesX[3] * xAmountRight;

		xcomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		return xcomp;
	}

	Vector2 interpolateBetweenValues(float x, float y, Vector2[,] array)
	{
		float xcomp,ycomp;

		int xl = array.GetLength(0);
		int yl = array.GetLength(1);

		int topLeftX = (int)Mathf.Floor(x);
		int topLeftY = (int)Mathf.Floor(y);

		float xAmountRight = x - topLeftX;
		float xAmountLeft = 1.0f - xAmountRight;
		float yAmountBottom = y - topLeftY;
		float yAmountTop = 1.0f - yAmountBottom;

		Vector4 valuesX = Vector4.zero;

		if (isPointInsideArray(topLeftX,topLeftY,xl,yl))			{valuesX[0] = array[topLeftX, topLeftY].x;}
		if (isPointInsideArray(topLeftX + 1, topLeftY,xl,yl)) 		{valuesX[1] = array[topLeftX + 1, topLeftY].x;}
		if (isPointInsideArray(topLeftX, topLeftY + 1,xl,yl)) 		{valuesX[2] = array[topLeftX, topLeftY + 1].x;}
		if (isPointInsideArray(topLeftX + 1, topLeftY + 1,xl,yl)) 	{valuesX[3] = array[topLeftX + 1, topLeftY + 1].x;}
		for (int n=0; n<4; n++) {
			if (float.IsNaN(valuesX[n])) {valuesX[n] = 0f;}
			if (float.IsInfinity(valuesX[n])) {valuesX[n] = 0f;}
		}

		float averagedXTop = valuesX[0] * xAmountLeft + valuesX[1] * xAmountRight;
		float averagedXBottom = valuesX[2] * xAmountLeft + valuesX[3] * xAmountRight;

		xcomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		Vector4 valuesY = Vector4.zero;
		if (isPointInsideArray(topLeftX,topLeftY,xl,yl))			{valuesY[0] = array[topLeftX, topLeftY].y;}
		if (isPointInsideArray(topLeftX + 1, topLeftY,xl,yl)) 		{valuesY[1] = array[topLeftX + 1, topLeftY].y;}
		if (isPointInsideArray(topLeftX, topLeftY + 1,xl,yl)) 		{valuesY[2] = array[topLeftX, topLeftY + 1].y;}
		if (isPointInsideArray(topLeftX + 1, topLeftY + 1,xl,yl)) 	{valuesY[3] = array[topLeftX + 1, topLeftY + 1].y;}
		for (int n=0; n<4; n++) {
			if (float.IsNaN(valuesY[n])) {valuesY[n] = 0f;}
			if (float.IsInfinity(valuesY[n])) {valuesY[n] = 0f;}
		}

		averagedXTop = valuesY[0] * xAmountLeft + valuesY[1] * xAmountRight;
		averagedXBottom = valuesY[2] * xAmountLeft + valuesY[3] * xAmountRight;

		ycomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		return new Vector2(xcomp,ycomp);
	}


	bool isPointInsideArray(int x, int y, int xl, int yl) {
		if (x<0 || x>xl-1 || y<0 || y>yl-1) {
			return false;
		}
		return true;
	}
}
