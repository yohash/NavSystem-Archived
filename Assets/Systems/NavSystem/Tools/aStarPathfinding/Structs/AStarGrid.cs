using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AStarGrid's sole purpose is to take in the height map and discomfort map
// produced by the MapAnalyzer. Then, using a simple increasing resolution 
// square search, it approximates the map with a grid that will be used
// for AStar calculations. 


public struct AStarNode
{
	public int x, y;
	public int dim;

	public AStarNode (int xLoc, int yLoc, int dimension)
	{
		x = xLoc;
		y = yLoc;
		dim = dimension;
	}
}

public struct AStarNeighbor
{
	public float cost;
	public AStarNode theNode;

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

	public AStarGrid (float[,] h, float[,] g, int[] nodeSizes)
	{
		nodeNeighbors = new Dictionary<AStarNode,List<AStarNeighbor>> ();
		nodes = new List<AStarNode> ();

		int dim;

		int xMax = h.GetLength (0);
		int yMax = h.GetLength (1);

		// this portion finds suitable locations for all the nodes
		for (int k = 0; k < nodeSizes.Length; k++) {
			dim = nodeSizes [k];
			for (int n = dim; n < xMax - dim; n++) {
				for (int m = dim; m < xMax - dim; m++) {
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
			for (int k = i+1; k < nodes.Count; k++) {
				if (nodesAreNeighbors(nodes[i],nodes[k])) {
					addNodesToNodeNeighbors (nodes [i], nodes [k]);
				}
			}
			// if the node has no neighbors, add it to the dictionary with an empty list
			if (!nodeNeighbors.ContainsKey (nodes [i])) {
				List<AStarNeighbor> neighbors = new List<AStarNeighbor>();
				nodeNeighbors.Add (nodes [i], neighbors);
			}
		}
	}

	public bool nodeIsValidAtPoint (float[,] g, int x, int y, int dim)
	{
		// first, check if any spot in this possible node is inside a node already
		foreach (AStarNode n in nodes) {
			if (aStarNodeContainsTestNode (n, x, y, dim)) {
				return false;
			}
		}
		// next, check if any spot in the node is in unpassable terrain
		for (int n = x - dim; n < x + dim; n++) {
			for (int m = y - dim; m < y + dim; m++) {
				if (g [n, m] == 1)
					return false;
			}
		}
		// this two checks should determine a valid node
		return true;
	}

	public bool aStarNodeContainsTestNode (AStarNode an, int x, int y, int dim)
	{
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
		// if all the tests are false, then this aStarNode DOES contain the test node
		return true;
	}

	public bool nodesAreNeighbors (AStarNode an1, AStarNode an2)
	{
		// SUPER IMPORTANT NOTE:
		// THIS IS NOT A GOOD ENOUGH TEST
		// it doesnt consider nodes hopping walls
		// - we'll have to check top, bottom, R and L individually
		//   to see if(there are no g==1) && if(this boundary is inside another node)


		// best way to test (maybe) is make one node's dimension bigger
		// by 1, then use aStarNodeContainsTestNode.
		// if the answer is true, then they are neighbors
		if (aStarNodeContainsTestNode (an1, an2.x, an2.y, an2.dim + 1)) {
			return true;
		} else if (aStarNodeContainsTestNode (an1, an2.x, an2.y, an2.dim + 2)) {
			// THIS IS MY SUPER LAME WAY TO FIX SEQUENTIALLY SMALLER NODES OF ODD SIDE LENGTH
			// MISSING SIMPLE CONNECTIONS
			// IT WILL ACTUALLY BE PERMANENT ONCE WE TEST THIS CONDITION PROPERLY
			return true;
		}
		return false;
	}

	public void addNodesToNodeNeighbors(AStarNode an1, AStarNode an2) {
		// here, we need to add the nodes to each other's dictionary entries

		// first, calculate the cost between the two
		float cost = Mathf.Sqrt((an1.x - an2.x)*(an1.x - an2.x) + (an1.y - an2.y)*(an1.y - an2.y));

		// create a neighbor struct of each one
		AStarNeighbor an1Neigh = new AStarNeighbor(an1, cost);
		AStarNeighbor an2Neigh = new AStarNeighbor(an2, cost );

		List<AStarNeighbor> neighbors = new List<AStarNeighbor>();

		// next, see if an1 has a dictionary entry, and if so, copy it
		if (nodeNeighbors.ContainsKey (an1)) {
			neighbors = nodeNeighbors [an1];
			neighbors.Add (an2Neigh);
			nodeNeighbors[an1] = neighbors;
		} else {
			neighbors.Add (an2Neigh);
			nodeNeighbors.Add (an1, neighbors);
		}

		// do the same for an2
		neighbors = new List<AStarNeighbor>();
		if (nodeNeighbors.ContainsKey (an2)) {
			neighbors = nodeNeighbors [an2];
			neighbors.Add (an1Neigh);
			nodeNeighbors[an2] = neighbors;
		} else {
			neighbors.Add (an1Neigh);
			nodeNeighbors.Add (an2, neighbors);
		}
	}
}
