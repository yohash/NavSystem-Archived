using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AStarGrid's sole purpose is to take in the height map and discomfort map
// produced by the MapAnalyzer. Then, using a simple increasing resolution
// square search, it approximates the map with a grid that will be used
// for AStar calculations.


public struct AStarNode
{
	// this is essentially a (int) version of a Rect
	// lower-left corner
	private int xCorner, yCorner;
	// width and height from lower-left corner
	private int width, height;

	// the CENTER x and y (these are floats)
	public float x, y;

	public AStarNode (int xLoc, int yLoc, int dimension)
	{
		xCorner = xLoc;
		yCorner = yLoc;
		width = dimension;
		height = dimension;
		x = (xCorner + (width+1) / 2f);
		y = (yCorner + (height+1) / 2f);
	}

	public AStarNode (int xLoc, int yLoc, int w, int h)
	{
		xCorner = xLoc;
		yCorner = yLoc;
		width = w;
		height = h;
		x = (xCorner + (width+1) / 2f);
		y = (yCorner + (height+1) / 2f);
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
		return xCorner;
	}

	public int getYCorner ()
	{
		return yCorner;
	}

	public int getHeight ()
	{
		return height;
	}

	public int getWidth ()
	{
		return width;
	}
}

public struct AStarNeighbor
{
	public AStarNode theNode;
	public float cost;

	public AStarNeighbor (AStarNode n, float c)
	{
		theNode = n;
		cost = c;
	}
}

public struct AStarGrid
{
	public Dictionary<AStarNode,List<AStarNeighbor>> nodeNeighbors;
	public List<AStarNode> nodes;

	private int xMax;
	private int yMax;

	public AStarGrid (float[,] h, float[,] g, int[] nodeSizes)
	{
		nodeNeighbors = new Dictionary<AStarNode,List<AStarNeighbor>> ();
		nodes = new List<AStarNode> ();

		int dim;

		xMax = h.GetLength (0);
		yMax = h.GetLength (1);

		// this portion finds suitable locations for all the nodes
		for (int k = 0; k < nodeSizes.Length; k++) {
			dim = nodeSizes [k];
			for (int n = 0; n < xMax - dim; n++) {
				for (int m = 0; m < yMax - dim; m++) {
					if (nodeIsValidAtPoint (g, n, m, dim)) {
						nodes.Add (new AStarNode (n, m, dim));
					}
				}
			}
		}

		int num = 0;
		// this portion fills empty holes and gaps by dimension by
		// dimension expanding each node
		foreach (AStarNode an in nodes) {
			num++;
			Debug.Log ("node #: " + num + "/" + nodes.Count + ", with index "+nodes.IndexOf (an));
			expandNodeBoundaries (g, an);
		}

		// This portion identifies a given node's neighbors.
		// Each node only needs to be compared to the others once.
		// Any further testing could produce redundancies.
		// This is an (N^2)/2 problem.
		for (int i = 0; i < nodes.Count; i++) {
			for (int k = i + 1; k < nodes.Count; k++) {
				if (nodesAreNeighbors (g, nodes [i], nodes [k])) {
					addNodesToNodeNeighbors (nodes [i], nodes [k]);
				}
			}
			// if the node has no neighbors, add it to the dictionary with an empty list
			if (!nodeNeighbors.ContainsKey (nodes [i])) {
				List<AStarNeighbor> neighbors = new List<AStarNeighbor> ();
				nodeNeighbors.Add (nodes [i], neighbors);
			}
		}
	}

	private bool nodeIsValidAtPoint (float[,] g, int x, int y, int dim)
	{
		// first, check if any spot in this possible node is inside a node already
		foreach (AStarNode n in nodes) {
			if (aStarNodeContainsTestNode (n, x, y, dim)) {
				return false;
			}
		}
		// next, check if any spot in the node is in unpassable terrain
		// use <= to make sure we go right to the edge of the node
		for (int n = x; n <= x + dim; n++) {
			for (int m = y; m <= y + dim; m++) {
				if (g [n, m] == 1)
					return false;
			}
		}
		// this two checks should determine a valid node
		return true;
	}

	private bool aStarNodeContainsTestNode (AStarNode an, int x, int y, int dim)
	{
		// perform basic two-rectangle collision text
		// if ANY of the following cases are true, then the rectangles CANNOT overlap
		// (1) if rect1 bottom higher than rect2 top - CANNOT OVERLAP
		// (2) if rect1 top lower than rect2 bottom - CANNOT OVERLAP
		// (3) if rect1 left greater than rect2 right - CANNOT OVERLAP
		// (4) if rect1 right less than rect2 left - CANNOT OVERLAP
		if ((y) > (an.getYCorner () + an.getHeight ()) ||
		    (y + dim) < (an.getYCorner ()) ||
		    (x) > (an.getXCorner () + an.getWidth ()) ||
		    (x + dim) < (an.getXCorner ())) {
			return false;
		}
		// if all the tests are false, then this aStarNode DOES contain the test node
		return true;
	}

	private bool aStarNodeContainsTestPoint (AStarNode an, int x, int y)
	{
		return aStarNodeContainsTestNode (an, x, y, 0);
	}

	private bool ANYaStarNodesContainTestPoint (int x, int y)
	{
		foreach (AStarNode an in nodes) {
			if (aStarNodeContainsTestPoint (an, x, y)) {
				return true;
			}
		}
		return false;
	}

	private void expandNodeBoundaries (float[,] g, AStarNode thisNode)
	{
		// test to expand boundaries
		// (1) 	at [top, bottom, left, right]
		// 		check [above, below, left, right]
		//		of each square at each edge
		// (2)	if (unpathable) isExtendable = false;
		// (3)  if (point is inside another node) isExtendable = false;
		// (4)	if (isExtendable) extend node by 1 row in current direction

		AStarNode an = thisNode;

		// check to the right
		bool isExtendable = true;
		while (isExtendable) {
			// scan over the height of the node
			for (int m = an.getYCorner (); m <= (an.getYCorner () + an.getHeight ()); m++) {
				int x = an.getXCorner () + an.getWidth () + 1;
				// make sure this edge isnt off the map
				if (pointIsInBounds (x, m)) { 
					// check if the spot is unpathable
					if (g [x, m] == 1) {
						isExtendable = false;
					}
					if (ANYaStarNodesContainTestPoint (x, m)) {
						isExtendable = false;
					}
				} else {
					isExtendable = false;
				}
			}
			if (isExtendable) {
				int w = an.getWidth () + 1;
				an = new AStarNode (an.getXCorner (), an.getYCorner (), w, an.getHeight ());
			}
		}
		// check to the left
		isExtendable = true;
		while (isExtendable) {
			// scan over the height of the node
			for (int m = an.getYCorner (); m <= (an.getYCorner () + an.getHeight ()); m++) {
				int x = an.getXCorner () - 1;
				// make sure this edge isnt off the map
				if (pointIsInBounds (x, m)) {
					// check if the spot is unpathable
					if (g [x, m] == 1) {
						isExtendable = false;
					}
					if (ANYaStarNodesContainTestPoint (x, m)) {
						isExtendable = false;
					}
				} else {
					isExtendable = false;
				}
			}
			if (isExtendable) {
				int xc = an.getXCorner () - 1;
				int w = an.getWidth () + 1;
				an = new AStarNode (xc, an.getYCorner (), w, an.getHeight ());
			}
		}
		// check up
		isExtendable = true;
		while (isExtendable) {
			// scan over the height of the node
			for (int n = an.getXCorner (); n <= (an.getXCorner () + an.getWidth ()); n++) {
				int y = an.getYCorner () + an.getHeight () + 1;
				// make sure this edge isnt off the map
				if (pointIsInBounds (n, y)) {
					// check if the spot is unpathable
					if (g [n, y] == 1) {
						isExtendable = false;
					}
					if (ANYaStarNodesContainTestPoint (n, y)) {
						isExtendable = false;
					}
				} else {
					isExtendable = false;
				}
			}
			if (isExtendable) {
				int i = nodes.IndexOf (an);
				int h = an.getHeight () + 1;
				an = new AStarNode (an.getXCorner (), an.getYCorner (), an.getWidth (), h);
			}
		}
		// check down
		isExtendable = true;
		while (isExtendable) {
			// scan over the height of the node
			for (int n = an.getXCorner (); n <= (an.getXCorner () + an.getWidth ()); n++) {
				int y = an.getYCorner () - 1;
				// make sure this edge isnt off the map
				if (pointIsInBounds (n, y)) {
					// check if the spot is unpathable
					if (g [n, y] == 1) {
						isExtendable = false;
					}
					if (ANYaStarNodesContainTestPoint (n, y)) {
						isExtendable = false;
					}
				} else {
					isExtendable = false;
				}
			}
			if (isExtendable) {
				int yc = an.getYCorner () - 1;
				int h = an.getHeight () + 1;
				an = new AStarNode (an.getXCorner (), yc, an.getWidth (), h);
			}
		}
		if (an != thisNode) {
			int i = nodes.IndexOf (thisNode);
			nodes [i] = an;
		}
	}

	private bool nodesAreNeighbors (float[,] g, AStarNode an1, AStarNode an2)
	{
		// test to discern neighborhood
		// (1) 	at [top, bottom, left, right]
		// 		check [above, below, left, right] of each square at each edge
		// (2)  if (point is inside another node) nodes are neighbors

		// check to the right - scan over the height of the node
		for (int m = an1.getYCorner (); m <= (an1.getYCorner () + an1.getHeight ()); m++) {
			int x = an1.getXCorner () + an1.getWidth () + 1;
			// make sure this edge isnt off the map && check if the point is in the other node
			if (pointIsInBounds (x, m) && aStarNodeContainsTestPoint (an2, x, m)) {
				return true;
			}
		}
		// check to the left - scan over the height of the node
		for (int m = an1.getYCorner (); m <= (an1.getYCorner () + an1.getHeight ()); m++) {
			int x = an1.getXCorner () - 1;
			// make sure this edge isnt off the map && check if the point is in the other node
			if (pointIsInBounds (x, m) && aStarNodeContainsTestPoint (an2, x, m)) {
				return true;
			}
		}
		// check up - scan over the width of the node
		for (int n = an1.getXCorner (); n <= (an1.getXCorner () + an1.getWidth ()); n++) {
			int y = an1.getYCorner () + an1.getHeight () + 1;
			// make sure this edge isnt off the map && check if the point is in the other node
			if (pointIsInBounds (n, y) && aStarNodeContainsTestPoint (an2, n, y)) {
				return true;
			} 
		}
		// check down - scan over the width of the node
		for (int n = an1.getXCorner (); n <= (an1.getXCorner () + an1.getWidth ()); n++) {
			int y = an1.getYCorner () - 1;
			// make sure this edge isnt off the map && check if the point is in the other node
			if (pointIsInBounds (n, y) && aStarNodeContainsTestPoint (an2, n, y)) {
				return true;
			}
		}

		return false;
	}

	private void addNodesToNodeNeighbors (AStarNode an1, AStarNode an2)
	{	// here, we need to add the nodes to each other's dictionary entries

		// first, calculate the cost between the two
		float cost = Mathf.Sqrt ((an1.x - an2.x) * (an1.x - an2.x) + (an1.y - an2.y) * (an1.y - an2.y));

		// create a neighbor struct of each one
		AStarNeighbor an1Neigh = new AStarNeighbor (an1, cost);
		AStarNeighbor an2Neigh = new AStarNeighbor (an2, cost);

		List<AStarNeighbor> neighbors = new List<AStarNeighbor> ();

		// next, see if an1 has a dictionary entry, and if so, copy it
		if (nodeNeighbors.ContainsKey (an1)) {
			neighbors = nodeNeighbors [an1];
			neighbors.Add (an2Neigh);
			nodeNeighbors [an1] = neighbors;
		} else {
			neighbors.Add (an2Neigh);
			nodeNeighbors.Add (an1, neighbors);
		}

		// do the same for an2
		neighbors = new List<AStarNeighbor> ();
		if (nodeNeighbors.ContainsKey (an2)) {
			neighbors = nodeNeighbors [an2];
			neighbors.Add (an1Neigh);
			nodeNeighbors [an2] = neighbors;
		} else {
			neighbors.Add (an1Neigh);
			nodeNeighbors.Add (an2, neighbors);
		}
	}


	private bool pointIsInBounds (int x, int y)
	{
		if ((x < 0) || (y < 0) || (x > xMax - 1) || (y > yMax - 1)) {
			return false;
		}
		return true;
	}
}
