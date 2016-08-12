using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct AStarNeighbor {
	public float cost;
	public AStarNode theNode;

	public AStarNeighbor(float c, AStarNode n) {
		cost = c;
		theNode = n;
	}
}

public struct AStarNode {
	public int x,y;
	public int dim;

	public AStarNode(int xLoc, int yLoc, int dimension) {
		x = xLoc;
		y = yLoc;
		dim = dimension;
	}
}

public struct AStarGrid {
	
	public Dictionary<AStarNode,AStarNeighbor> nodeNeighbors;
	public List<AStarNode> nodes;

	public AStarGrid(float[,] h, float[,] g, int[] nodeSizes) {

		nodeNeighbors = new Dictionary<AStarNode,AStarNeighbor> ();
		nodes = new List<AStarNode> ();

		int dim;

		int xMax = h.GetLength (0);
		int yMax = h.GetLength (1);

		// this portion finds suitable locations for all the nodes
		for (int k = 0; k < nodeSizes.Length; k++) {
			dim = nodeSizes [k];
			for (int n = dim; n < xMax-dim; n++) {
				for (int m = dim; m < xMax-dim; m++) {
					if (nodeIsValidAtPoint (g, n, m, dim)) {
						nodes.Add (new AStarNode (n, m, dim));
					}
				}
			}
		}

		// This portion identifies a given node's neighbors.
		// Each node only needs to be compared to the others once.
		// Any further testing could produce redundancies.
		// This is an (N^2)/2 problem.
		for (int i = 0; i < nodes.Count; i++) {
			for (int k = i; k < nodes.Count; i++) {

			}
		}
	}

	public bool nodeIsValidAtPoint(float[,] g, int x, int y, int dim) {
		// first, check if any spot in this possible node is inside a node already
		foreach (AStarNode n in nodes) {
			if (aStarNodeContainsTestNode (n, x, y, dim)) {
				return false;
			}
		}
		// next, check if any spot in the node is in unpassable terrain
		for (int n = x-dim; n < x+dim; n++) {
			for (int m = y-dim; m < y+dim; m++) {
				if (g [n, m] == 1)
					return false;
			}
		}
		// this two checks should determine a valid node
		return true;
	}

	public bool aStarNodeContainsTestNode(AStarNode an, int x, int y, int dim) {
		// perform basic two-rectangle collision text
		// if ANY of the following cases are true, then the rectangles CANNOT overlap
		// (1) if rect1 bottom higher than rect2 top - CANNOT OVERLAP
		// (2) if rect1 top lower than rect2 bottom - CANNOT OVERLAP
		// (3) if rect1 left greater than right of rect2 right - CANNOT OVERLAP
		// (4) if rect1 right less than left of rect2 left - CANNOT OVERLAP
		if ((y - dim) > (an.y + an.dim) ||
		    (y + dim) < (an.y - an.dim) ||
		    (x - dim) > (an.x + an.dim) ||
		    (x + dim) < (an.x - an.dim)) {
			return false;
		}

		return true;
	}

	public bool nodesAreNeighbors(AStarNode an1, AStarNode an2) {

	}
}
