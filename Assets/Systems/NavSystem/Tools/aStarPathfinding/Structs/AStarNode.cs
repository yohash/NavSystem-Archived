using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AStarNode is defined like an int version of a rect. 
// It has a lower corner and width and height.
// the center of this rect (x,y) is recalculated every time the dimensions
// are defined, and is publicly accessible for easy referencing.

public struct AStarNode
{
	// this is essentially a (int) version of a Rect
	// lower-left corner
	private int _xCorner, _yCorner;
	// width and height from lower-left corner
	private int _width, _height;

	// the CENTER x and y (these are floats)
	public float x, y;

	public AStarNode (int xLoc, int yLoc, int dimension)
	{
		_xCorner = xLoc;
		_yCorner = yLoc;
		_width = dimension;
		_height = dimension;
		x = (_xCorner + (_width+1) / 2f);
		y = (_yCorner + (_height+1) / 2f);
	}

	public AStarNode (int xLoc, int yLoc, int w, int h)
	{
		_xCorner = xLoc;
		_yCorner = yLoc;
		_width = w;
		_height = h;
		x = (_xCorner + (_width+1) / 2f);
		y = (_yCorner + (_height+1) / 2f);
	}

	public static bool operator == (AStarNode l1, AStarNode l2)
	{
		return((l1.getXCorner () == l2.getXCorner ()) &&
			(l1.getYCorner () == l2.getYCorner ()) &&
			(l1.getHeight () == l2.getHeight ()) &&
			(l1.getWidth () == l2.getWidth ()) &&
			(l1.x == l2.x) && (l1.y == l2.y)
		);
	}

	public static bool Equals (AStarNode l1, AStarNode l2)
	{
		return(l1==l2);
	}

	public static bool operator != (AStarNode l1, AStarNode l2)
	{
		return(!(l1 == l2));
	}

	public int getXCorner ()
	{
		return _xCorner;
	}

	public int getYCorner ()
	{
		return _yCorner;
	}

	public int getHeight ()
	{
		return _height;
	}

	public int getWidth ()
	{
		return _width;
	}
}