using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using Priority_Queue;

public interface WeightedGraph<L>
{
	float Cost(Location a, Location b);
	IEnumerable<Location> Neighbors(Location id);
}

public struct Location 
{
	public readonly int x, y;
	public Location(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	public static bool operator ==(Location l1, Location l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool Equals(Location l1, Location l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool operator !=(Location l1, Location l2) {
		return(!(l1==l2));
	}
}

public class SquareGrid : WeightedGraph<Location>
{

	public static readonly Location[] DIRS = new []
	{
		new Location(1, 0),
		new Location(0, -1),
		new Location(-1, 0),
		new Location(0, 1)
	};

	public int width, height;
	public HashSet<Location> walls = new HashSet<Location>();
	public Dictionary<Location, float> terrain = new Dictionary<Location, float>();

	public SquareGrid(int width, int height)
	{
		this.width = width;
		this.height = height;
	}

	public bool InBounds(Location id)
	{
		return 0 <= id.x && id.x < width && 0 <= id.y && id.y < height;
	}

	public bool Passable(Location id)
	{
		return !walls.Contains(id);
	}

	public float Cost(Location a, Location b)
	{
		return terrain.ContainsKey(b) ? (1 + terrain[b]-terrain[a]) : 1;
	}

	public IEnumerable<Location> Neighbors(Location id)
	{
		foreach (var dir in DIRS) {
			Location next = new Location(id.x + dir.x, id.y + dir.y);
			if (InBounds(next) && Passable(next)) {
				yield return next;
			}
		}
	}
}

// the A* pathfinding algorithm as implemented by www.redblobgames.com
public class AStarSearch {

	public Dictionary<Location, Location> cameFrom = new Dictionary<Location, Location>();
	public Dictionary<Location, float> costSoFar = new Dictionary<Location, float>();

	// Note: a generic version of A* would abstract over Location and
	// also Heuristic
	static public float Heuristic(Location a, Location b)
	{
		float dist = 
			Mathf.Sqrt(Mathf.Abs(a.x - b.x)*Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y)*Mathf.Abs(a.y - b.y));
	
		return dist;
	}

	public AStarSearch(WeightedGraph<Location> graph, Location start, Location goal)
	{
		SimplePriorityQueue<Location> frontier = new SimplePriorityQueue<Location>();
		frontier.Enqueue(start, 0);

		cameFrom[start] = start;
		costSoFar[start] = 0;

		while (frontier.Count > 0)
		{
			var current = frontier.Dequeue();
			if (current.Equals(goal)) {break;}
			foreach (var next in graph.Neighbors(current))
			{
				float newCost = costSoFar[current]	+ graph.Cost(current, next);
				if (!costSoFar.ContainsKey(next) || newCost < costSoFar[next])	{
					costSoFar[next] = newCost;
					float priority = newCost + Heuristic(next, goal);
					frontier.Enqueue(next, priority);
					cameFrom[next] = current;
				}
			}
		}
	}
}
