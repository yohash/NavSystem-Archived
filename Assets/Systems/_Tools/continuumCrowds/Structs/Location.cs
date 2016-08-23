using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public struct Location : IEquatable<Location>
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
	public int GetHashCode() {
		int hash = 17;
		hash = (31 * hash) + this.x;
		hash = (31 * hash) + this.y;
		return hash;
	}

	public static bool operator ==(Location l1, Location l2) {
		return((l1.x==l2.x) && (l1.y==l2.y));
	}
	public static bool operator !=(Location l1, Location l2) {
		return(!(l1==l2));
	}
}