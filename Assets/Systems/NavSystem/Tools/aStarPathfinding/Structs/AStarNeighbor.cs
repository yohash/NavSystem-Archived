using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// AStarNeighbor holds the node that is the neighbor, and the cost
// to get there.
// The purpose of the neighbor is to fit in a dictionary as such:
//			Dictionary <AStarNode, List<AStarNeighbor>>
// This way, the AStarNode and its list of neighbors pair up costs

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