using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public struct CC_Unit_Goal_Group
{
	public Rect goal;
	public List<CC_Unit> units;

	public CC_Unit_Goal_Group (Rect r, List<CC_Unit> u)
	{
		this.goal = r;
		this.units = u;
	}
}