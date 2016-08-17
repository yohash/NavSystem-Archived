using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Priority_Queue;


// the A* pathfinding algorithm as implemented by www.redblobgames.com
public class AStarSearch
{
	public Dictionary<AStarNode, AStarNode> cameFrom = new Dictionary<AStarNode, AStarNode> ();
	public Dictionary<AStarNode, float> costSoFar = new Dictionary<AStarNode, float> ();

	public AStarNode start, goal;

	// Note: a generic version of A* would abstract over Location and also Heuristic
	static public float Heuristic (AStarNode a, AStarNode b)
	{
		float absX = Mathf.Abs(a.x - b.x);
		float absY = Mathf.Abs(a.y - b.y);

		float dist = Mathf.Sqrt(absX*absX + absY*absY);		// distance
		return dist;
	}

	public AStarSearch (AStarGrid grid, Vector2 startLoc, Vector2 goalLoc)
	{
		AStarNode[] nodes = findNearestNodes (grid, startLoc, goalLoc);

		start = nodes [0];
		goal = nodes [1];

		SimplePriorityQueue<AStarNode> frontier = new SimplePriorityQueue<AStarNode> ();
		frontier.Enqueue (start, 0);

		cameFrom [start] = start;
		costSoFar [start] = 0;

		while (frontier.Count > 0) {
			AStarNode current = frontier.Dequeue ();
			if (current.Equals (goal)) {
				break;
			}
			foreach (AStarNeighbor an in grid.nodeNeighbors[current]) {
				float newCost = costSoFar [current]	+ an.cost;
				if (!costSoFar.ContainsKey (an.theNode) || newCost < costSoFar [an.theNode]) {
					costSoFar [an.theNode] = newCost;
					float priority = newCost + Heuristic (an.theNode, goal);
					frontier.Enqueue (an.theNode, priority);
					cameFrom [an.theNode] = current;
				}
			}
		}
	}

	// this finds the NEAREST NODES to the start and goal location
	// this will likely need to be modified later to make a more robust node-finder that
	// will not assign units 1-st node as a node on another height level (rounding error)
	private AStarNode[] findNearestNodes (AStarGrid grid, Vector2 s, Vector2 g)
	{
		float nearestS = 9999f, nearestG = 9999f;		
		float sMag, gMag;

		AStarNode startNode = new AStarNode (0, 0, 1);
		AStarNode goalNode = new AStarNode (0, 0, 1);

		foreach (AStarNode an in grid.nodes) {
			sMag = (s - new Vector2 (an.x, an.y)).sqrMagnitude;
			gMag = (g - new Vector2 (an.x, an.y)).sqrMagnitude;

			if (sMag < nearestS) {
				startNode = an;
				nearestS = sMag;
			}
			if (gMag < nearestG) {
				goalNode = an;
				nearestG = gMag;
			}
		}

		return new AStarNode[]{ startNode, goalNode };
	}
}
