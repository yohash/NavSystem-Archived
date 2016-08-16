using UnityEngine;
using System.Collections;

using Priority_Queue;

public class fastLocation : FastPriorityQueueNode
{
	public readonly int x, y;
	public fastLocation(int x, int y)
	{
		this.x = x;
		this.y = y;
	}
	public static bool operator ==(fastLocation l1, fastLocation l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool Equals(fastLocation l1, fastLocation l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool operator !=(fastLocation l1, fastLocation l2) {
		return(!(l1==l2));
	}
}
