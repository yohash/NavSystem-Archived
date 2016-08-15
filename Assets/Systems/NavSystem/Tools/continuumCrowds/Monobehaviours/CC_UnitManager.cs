using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Threading;
using Foundation.Tasks;


public class CC_UnitManager : MonoBehaviour
{
	// a CC_UnitManager holds lists of its units, and packages them
	// for submission into the ContinuumCrowds class

	public List<CC_Unit> My_CC_units;

	private CC_Map_Package My_CC_map_package;
	private List<CC_Unit_Goal_Group> My_CC_unit_goal_groups;

	CC_Unit_Goal_Group temp_unit_goal_group;
	List<CC_Unit> temp_cc_units;

	ContinuumCrowds CC;

	List<Vector2[,]> vel_fields;

	bool CC_1stIter_done = false;


	int ccGroup = 0;

	void Awake ()
	{
		My_CC_units.AddRange (GetComponentsInChildren<CC_Unit> ());
		My_CC_unit_goal_groups = new List<CC_Unit_Goal_Group> ();

		Rect r;
		r = new Rect(new Vector2(28,0), new Vector2(2,20));
		temp_cc_units = new List<CC_Unit>(); 
		temp_cc_units.Add( My_CC_units[0]); 
		temp_cc_units.Add( My_CC_units[1]); 
		temp_cc_units.Add( My_CC_units[2]); 
		temp_cc_units.Add( My_CC_units[3]); 
		temp_cc_units.Add( My_CC_units[4]); 
		temp_cc_units.Add( My_CC_units[5]); 
		temp_cc_units.Add( My_CC_units[6]); 
		temp_cc_units.Add( My_CC_units[7]); 
		temp_cc_units.Add( My_CC_units[8]);
		temp_cc_units.Add( My_CC_units[9]);
		temp_unit_goal_group = new CC_Unit_Goal_Group(r, temp_cc_units);

		My_CC_unit_goal_groups.Add(temp_unit_goal_group);

		r = new Rect(new Vector2(0,0), new Vector2(2,20));
		temp_cc_units = new List<CC_Unit>();
		temp_cc_units.Add( My_CC_units[10]); 
		temp_cc_units.Add( My_CC_units[11]); 
		temp_cc_units.Add( My_CC_units[12]); 
		temp_cc_units.Add( My_CC_units[13]); 
		temp_cc_units.Add( My_CC_units[14]); 
		temp_cc_units.Add( My_CC_units[15]); 
		temp_cc_units.Add( My_CC_units[16]); 
		temp_cc_units.Add( My_CC_units[17]); 
		temp_cc_units.Add( My_CC_units[18]);
		temp_cc_units.Add( My_CC_units[19]);
		temp_unit_goal_group = new CC_Unit_Goal_Group(r, temp_cc_units);

		My_CC_unit_goal_groups.Add(temp_unit_goal_group);
	}

	void Start () {
		Invoke("start_CC",0.5f);
	}
	void start_CC() {
		
		My_CC_map_package = new CC_Map_Package (
//			mapAnalyzer.S.get_dh (),
//			mapAnalyzer.S.get_h (), 
//			mapAnalyzer.S.get_g ()
		);

//		StartCoroutine("start_CC_task");
	}

	void Update() {
		if (CC_1stIter_done) {
			// distribute velocities to each unit in each unit-goal-group
			int index = 0;
			Vector2 newV;

			
//			foreach(CC_Unit_Goal_Group ccugg in My_CC_unit_goal_groups) {
			foreach(CC_Unit ccu in My_CC_unit_goal_groups[ccGroup].units) {
				newV = interpolateBetweenValues(ccu.getLocalPosition().x,ccu.getLocalPosition().y,vel_fields[ccGroup]);
					ccu.setVelocity(newV);
				}
				index++;
				ccGroup++;
				if (ccGroup >= My_CC_unit_goal_groups.Count) {
					ccGroup = 0;
				}
//			}
		}
	}




	IEnumerator start_CC_task() {
		while (true) {

			var task = UnityTask.Run(() =>
				{
					runCC();
				});

			yield return task; 
		}
	}

	void runCC() {
		CC = new ContinuumCrowds(My_CC_map_package, My_CC_unit_goal_groups);

		vel_fields = CC.vFields;

		CC_1stIter_done = true;
	}

	Vector2 interpolateBetweenValues(float x, float y, Vector2[,] array)
	{
		float xcomp,ycomp;

		int xl = array.GetLength(0);
		int yl = array.GetLength(1);

		int topLeftX = (int)Mathf.Floor(x);
		int topLeftY = (int)Mathf.Floor(y);

		float xAmountRight = x - topLeftX;
		float xAmountLeft = 1.0f - xAmountRight;
		float yAmountBottom = y - topLeftY;
		float yAmountTop = 1.0f - yAmountBottom;

		Vector4 valuesX = Vector4.zero;

		if (isPointInsideArray(topLeftX,topLeftY,xl,yl))			{valuesX[0] = array[topLeftX, topLeftY].x;}
		if (isPointInsideArray(topLeftX + 1, topLeftY,xl,yl)) 		{valuesX[1] = array[topLeftX + 1, topLeftY].x;}
		if (isPointInsideArray(topLeftX, topLeftY + 1,xl,yl)) 		{valuesX[2] = array[topLeftX, topLeftY + 1].x;}
		if (isPointInsideArray(topLeftX + 1, topLeftY + 1,xl,yl)) 	{valuesX[3] = array[topLeftX + 1, topLeftY + 1].x;}
		for (int n=0; n<4; n++) {
			if (float.IsNaN(valuesX[n])) {valuesX[n] = 0f;}
			if (float.IsInfinity(valuesX[n])) {valuesX[n] = 0f;}
		}

		float averagedXTop = valuesX[0] * xAmountLeft + valuesX[1] * xAmountRight;
		float averagedXBottom = valuesX[2] * xAmountLeft + valuesX[3] * xAmountRight;

		xcomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		Vector4 valuesY = Vector4.zero;
		if (isPointInsideArray(topLeftX,topLeftY,xl,yl))			{valuesY[0] = array[topLeftX, topLeftY].y;}
		if (isPointInsideArray(topLeftX + 1, topLeftY,xl,yl)) 		{valuesY[1] = array[topLeftX + 1, topLeftY].y;}
		if (isPointInsideArray(topLeftX, topLeftY + 1,xl,yl)) 		{valuesY[2] = array[topLeftX, topLeftY + 1].y;}
		if (isPointInsideArray(topLeftX + 1, topLeftY + 1,xl,yl)) 	{valuesY[3] = array[topLeftX + 1, topLeftY + 1].y;}
		for (int n=0; n<4; n++) {
			if (float.IsNaN(valuesY[n])) {valuesY[n] = 0f;}
			if (float.IsInfinity(valuesY[n])) {valuesY[n] = 0f;}
		}

		averagedXTop = valuesY[0] * xAmountLeft + valuesY[1] * xAmountRight;
		averagedXBottom = valuesY[2] * xAmountLeft + valuesY[3] * xAmountRight;

		ycomp = averagedXTop * yAmountTop + averagedXBottom * yAmountBottom;

		return new Vector2(xcomp,ycomp);
	}


	bool isPointInsideArray(int x, int y, int xl, int yl) {
		if (x<0 || x>xl-1 || y<0 || y>yl-1) {
			return false;
		}
		return true;
	}
}