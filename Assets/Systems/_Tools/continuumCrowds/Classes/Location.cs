using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class Location : IEquatable<Location>
{
	public readonly int x, y;
	public Location(int x, int y)
	{
		this.x = x;
		this.y = y;
	}

	public bool Equals(Location l2) {
		return((this.x == l2.x) && (this.y == l2.y));
	}
	public override bool Equals(object obj) {
		Location loc = obj as Location;
		return Equals (loc);
	}
	public int GetHashCode(Location l) {
		int hash = 17;
		hash = (31 * hash) + l.x;
		hash = (31 * hash) + l.y;
		return hash;
	}

	public static bool operator ==(Location l1, Location l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool operator !=(Location l1, Location l2) {
		return(!(l1==l2));
	}
}