using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

// the CM_Unit_Goal_Groups struct is explicitly for the 
// combat manager to group his units into packages 
// that will easily interface with NavSystem means
// of solving ContinuumCrowd spaces

public struct CM_Unit_Goal_Groups {

	public List<Unit> units;
	public List<Rect> goals;
	public Rect unitGoalSolutionSpace;

	public Vector2[,] velocityField;

	float updateTime;
	public float timeUntilNextUpdate;

	public CM_Unit_Goal_Groups(List<Unit> u, List<Rect>  loc) {
		units = u;
		goals = loc;

		if (units.Count == 0 || goals.Count == 0) {
			Debug.Log ("ERROR - CM trying to assign no units or no goal to CM_Unit_Goal_Group");	
		}

		unitGoalSolutionSpace = new Rect ();

		velocityField = new Vector2[2, 2];
		updateTime = 0f;
		timeUntilNextUpdate = 0f;
	}

	// **********************************************************************************************************
	//			PUBLIC ACCESSORS
	// **********************************************************************************************************
	public void addUnitToGroup(Unit u) {
		units.Add (u);
	}
	public void setGoalList(List<Rect> r) {
		goals = r;
	}

	public void setVelocityField(Vector2[,] v) {
		velocityField = v;
		updateTime = Time.realtimeSinceStartup;
	}

	public float getTimeSinceLastUpdate() {
		return (Time.realtimeSinceStartup - updateTime);
	}

	public bool unitGoalGroupNeedsUpdate() {
		if (getTimeSinceLastUpdate () < timeUntilNextUpdate) {
			return false;
		} else {
			return true;
		}
	}

	public void reBoundUnitsAndGoals(float buffer) {
		Rect r = new Rect (units [0].getPosition(), new Vector2 (1, 1));

		foreach (Unit u in units) {
			Vector2 uPos = u.getPosition () - u.getSize () / 2f;
			Rect ru = new Rect (uPos, u.getSize ());
			r = reBoundTwoRects (r, ru);
		}
		foreach (Rect goal in goals) {
			r = reBoundTwoRects (r, goal);
		}

		int x, y, w, h;
		x = Mathf.FloorToInt (r.x - buffer);
		if (x < 0) {
			x = 0;
		}
		y = Mathf.FloorToInt (r.y - buffer);
		if (y < 0) {
			y = 0;
		}
		w = Mathf.CeilToInt (r.width + buffer * 2);
		h = Mathf.CeilToInt (r.height + buffer * 2);

		r = new Rect (x,y,w,h);

		unitGoalSolutionSpace = r;
	}

	public void setUnitVelocities() {
		foreach (Unit u in units) {
			Vector2 up = u.getPosition ();

			int xs = Mathf.FloorToInt(unitGoalSolutionSpace.x);
			int ys = Mathf.FloorToInt(unitGoalSolutionSpace.y);

			int xf = Mathf.CeilToInt (unitGoalSolutionSpace.x + unitGoalSolutionSpace.width);
			int yf = Mathf.CeilToInt (unitGoalSolutionSpace.y + unitGoalSolutionSpace.height);

			up -= new Vector2 (xs, ys);

			u.setDesiredVelocity(interpolateBetweenValues(up.x,up.y,velocityField));
		}
	}


	// **********************************************************************************************************
	//			MATH HELPERS
	// **********************************************************************************************************
	Rect reBoundTwoRects(Rect r1, Rect r2) {
		float x = r2.x, y = r2.y, w , h;

		if (r1.x < r2.x) {
			x = r1.x;
		}
		if (r1.y < r2.y) {
			y = r1.y;
		} 
		float xtop1 = r1.x + r1.width;
		float xtop2 = r2.x + r2.width;
		float ytop1 = r1.y + r1.height;
		float ytop2 = r2.y + r2.height;

		if (xtop1 > xtop2) {
			w = xtop1 - x;
		} else {
			w = xtop2 - x;
		}

		if (ytop1 > ytop2) {
			h = ytop1 - y;
		} else {
			h = ytop2 - y;
		}

		return (new Rect (x, y, w, h));
	}


	Vector2 interpolateBetweenValues (float x, float y, Vector2[,] array)
	{
		float xcomp, ycomp;

		int xl = array.GetLength (0);
		int yl = array.GetLength (1);

		int topLeftX = (int)Mathf.Floor (x);
		int topLeftY = (int)Mathf.Floor (y);

		float xAmountRight = x - topLeftX;
		float xAmountLeft = 1.0f - xAmountRight;
		float yAmountBottom = y - topLeftY;
		float yAmountTop = 1.0f - yAmountBottom;

		Vector4 valuesX = Vector4.zero;

		if (isPointInsideArray (topLeftX, topLeftY, xl, yl)) {
			valuesX [0] = array [topLeftX, topLeftY].x;
		}
		if (isPointInsideArray (topLeftX + 1, topLeftY, xl, yl)) {
			valuesX [1] = array [topLeftX + 1, topLeftY].x;
		}
		if (isPointInsideArray (topLeftX, topLeftY + 1, xl, yl)) {
			valuesX [2] = array [topLeftX, topLeftY + 1].x;
		}
		if (isPointInsideArray (topLeftX + 1, topLeftY + 1, xl, yl)) {
			valuesX [3] = array [topLeftX + 1, topLeftY + 1].x;
		}
		for (int n = 0; n < 4; n++) {
			if (float.IsNaN (valuesX [n])) {
				valuesX [n] = 0f;
			}
			if (float.IsInfinity (valuesX [n])) {
				valuesX [n] = 0f;
			}
		}

		float averagedXTop = valuesX [0] * xAmountLeft + valuesX [1] * xAmountRight;
		float averagedXBottom = valuesX [2] * xAmountLeft + valuesX [3] * xAmountRight;

		xcomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		Vector4 valuesY = Vector4.zero;
		if (isPointInsideArray (topLeftX, topLeftY, xl, yl)) {
			valuesY [0] = array [topLeftX, topLeftY].y;
		}
		if (isPointInsideArray (topLeftX + 1, topLeftY, xl, yl)) {
			valuesY [1] = array [topLeftX + 1, topLeftY].y;
		}
		if (isPointInsideArray (topLeftX, topLeftY + 1, xl, yl)) {
			valuesY [2] = array [topLeftX, topLeftY + 1].y;
		}
		if (isPointInsideArray (topLeftX + 1, topLeftY + 1, xl, yl)) {
			valuesY [3] = array [topLeftX + 1, topLeftY + 1].y;
		}
		for (int n = 0; n < 4; n++) {
			if (float.IsNaN (valuesY [n])) {
				valuesY [n] = 0f;
			}
			if (float.IsInfinity (valuesY [n])) {
				valuesY [n] = 0f;
			}
		}

		averagedXTop = valuesY [0] * xAmountLeft + valuesY [1] * xAmountRight;
		averagedXBottom = valuesY [2] * xAmountLeft + valuesY [3] * xAmountRight;

		ycomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		return (new Vector2 (xcomp, ycomp));
	}


	bool isPointInsideArray (int x, int y, int xl, int yl)
	{
		if (x < 0 || x > xl - 1 || y < 0 || y > yl - 1) {
			return false;
		}
		return true;
	}
}