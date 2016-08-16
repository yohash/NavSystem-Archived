using UnityEngine;
using System.Collections;


public struct CC_Map_Package
{
	public float[,] g;
	public Vector4[,] f, C;

	public CC_Map_Package (float[,] _g, Vector4[,] _f, Vector4[,] _C)
	{
		this.g = _g;
		this.f = _f;
		this.C = _C;
	}
}