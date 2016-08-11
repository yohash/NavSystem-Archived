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
//
//// this is custom written from redbloblgames.com 
//// this is not the most efficient implementation of a priority queue
//// best to try and use the one in C5 generic collection library
//// or next best thing to it
//public class PriorityQueue<T>
//{
//	private List<Tuple<T, double>> elements = new List<Tuple<T, double>>();
//
//	public int Count
//	{
//		get { return elements.Count; }
//	}
//
//	public void Enqueue(T item, double priority)
//	{
//		elements.Add(Tuple.New(item, priority));
//	}
//
//	public T Dequeue()
//	{
//		int bestIndex = 0;
//		for (int i = 0; i < elements.Count; i++) {
//			if (elements[i].Second < elements[bestIndex].Second) {
//				bestIndex = i;
//			}
//		}
//		T bestItem = elements[bestIndex].First;
//		elements.RemoveAt(bestIndex);
//		return bestItem;
//	}
//}
//// since Unity doesnt have Tuples, we have to implement
//// our own Tuple class as well
//public class Tuple<T1, T2>
//{
//	public T1 First { get; private set; }
//	public T2 Second { get; private set; }
//	internal Tuple(T1 first, T2 second)
//	{
//		First = first;
//		Second = second;
//	}
//}
//public static class Tuple
//{
//	public static Tuple<T1, T2> New<T1, T2>(T1 first, T2 second)
//	{
//		var tuple = new Tuple<T1, T2>(first, second);
//		return tuple;
//	}
//}

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
