using UnityEngine;
using System.Collections;

public class Location
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