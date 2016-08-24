using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour {

	public bool DEBUG_EIKONAL_SOLN = false;

	public List<Unit> myUnits;

	public Dictionary<Unit, float> unitPriorities;
	public Dictionary<Unit, float> enemyPriorities;

	public List<CC_Unit> myUnits_CCU;
	public List<CM_Unit_Goal_Group> unitGoalGroups;

	// until paths are implemented, use this
	public List<Vector2> myCurrentPath;
	public Queue<Vector2> currentPath;

	public Vector2 nextGoal;
	float minDistToStop = 0.5f;
	float minDistForNewPointSq;

	public float floatHeight;

	// based on experiments updating the Eikonal solver at different rates, it
	// seems that update rates ~0.5s produce best predictability without looking
	// 'wooden' and artificial. Very short times (<0.25s) will cause over-compensation
	// and some oscillation type behaviours
	public float CC_VelocityFieldUpdateFPS;
	public float CC_VelocityFieldUpdateTime;

	public float UnitVelocityUpdateFPS;
	public float UnitVelocityUpdateTime;

	float baseSolutionSpaceBuffer = 5f;
	float basicGoalDimension = 6f;

	public Rect goal;
	List<Rect> goalList;

	Vector2[,] vField;

	// cache the Eikonal Solver
	CCEikonalSolver cce;

	Transform tr;

	public Vector3 getPosition() {
		return tr.position;
	}

	void Awake () {
		tr = transform;
	}

	void Start () {
		unitPriorities = new Dictionary<Unit, float> ();
		enemyPriorities = new Dictionary<Unit, float> ();
		unitGoalGroups = new List<CM_Unit_Goal_Group> ();
		myUnits_CCU = new List<CC_Unit> ();

		vField = new Vector2[1, 1];

		currentPath = new Queue<Vector2> ();
		goalList = new List<Rect> ();

		minDistForNewPointSq = (basicGoalDimension * basicGoalDimension) * 8;
		CC_VelocityFieldUpdateTime = 1 / CC_VelocityFieldUpdateFPS;
		UnitVelocityUpdateTime = 1 / UnitVelocityUpdateFPS;
		StartCoroutine ("CC_VelocityFieldUpdate");
		StartCoroutine ("UnitVelocityUpdate");
	}

	void FixedUpdate () {
		Vector3 newPos;
		Vector2 ave = getUnitAveragePosition ();
		newPos = new Vector3(ave.x, NavSystem.S.getHeightAtPoint(ave.x,ave.y)+floatHeight , ave.y);
		tr.position = newPos;
	}

	// **********************************************************************************************************
	//			PUBLIC ACCESSORS
	// **********************************************************************************************************
	public void setCurrentPath(List<Vector2> newPath) {
		myCurrentPath = newPath;
		currentPath = new Queue<Vector2> ();

		foreach (Vector2 v in newPath) {
			currentPath.Enqueue (v);
		}
		if (currentPath.Count > 0) {
			setCurrentMoveTarget (currentPath.Dequeue ());
			setAllGoalLists ();
			StartCoroutine (Immediate_CC_Vel_Change ());
		} else {
			setCurrentMoveTarget (Vector2.zero);
		}
	}

	public void addUnitToCombatManagerGroup(Unit u) {
		myUnits.Add (u);

		if (unitGoalGroups.Count == 0) {
			CM_Unit_Goal_Group cmugg = new CM_Unit_Goal_Group (myUnits, getCurrentMoveTarget ());
			unitGoalGroups.Add (cmugg);
		} else {
			unitGoalGroups [0].addUnitToGroup (u);
		}
	}

	// **********************************************************************************************************
	//			CO-ROUTINES AND THREADING OPERATIONS
	// **********************************************************************************************************
	IEnumerator CC_VelocityFieldUpdate() {
		while (true) {
			if (nextGoal != Vector2.zero) {
				// check to see how far from the next point we are
				Vector2 currentLocV2 = new Vector2(tr.position.x, tr.position.z);
				float distSq = (currentLocV2 - nextGoal).sqrMagnitude;

				// check to see if we need a new target assigned
				if (distSq < minDistForNewPointSq) {					
					if (currentPath.Count > 0) {
						setCurrentMoveTarget (currentPath.Dequeue ());
						setAllGoalLists ();
					} else {
//						setCurrentMoveTarget (Vector2.zero);
					}
				}

				// update the unit-goal-groups that request an update
				foreach (CM_Unit_Goal_Group cmugg in unitGoalGroups) {
					if (cmugg.unitGoalGroupNeedsUpdate ()) {
						cmugg.reBoundUnitsAndGoals (baseSolutionSpaceBuffer);
						yield return StartCoroutine (_computeCCVelocityField (cmugg));
					}
				}
			}
			yield return new WaitForSeconds (CC_VelocityFieldUpdateTime * Random.Range(0.8f,1.2f));
		}
	}

	IEnumerator Immediate_CC_Vel_Change() {
		if (nextGoal != Vector2.zero) {
			// update the unit-goal-groups that request an update
			foreach (CM_Unit_Goal_Group cmugg in unitGoalGroups) {	
				cmugg.reBoundUnitsAndGoals (baseSolutionSpaceBuffer);
				yield return StartCoroutine (_computeCCVelocityField (cmugg));
			}
		}
	}

	IEnumerator _computeCCVelocityField(CM_Unit_Goal_Group cmugg) {
		cce = new CCEikonalSolver (
			NavSystem.S.getCCMapPackageFromRect (cmugg.unitGoalSolutionSpace), 
			NavSystem.S.getGoalAsLocations (cmugg.unitGoalSolutionSpace, cmugg.goals)
		);
		yield return StartCoroutine (cce.WaitFor ());

		cmugg.setVelocityField (cce.v);

		if (DEBUG_EIKONAL_SOLN) {			
			int xs = Mathf.FloorToInt (cmugg.unitGoalSolutionSpace.x);
			int ys = Mathf.FloorToInt (cmugg.unitGoalSolutionSpace.y);
			NavSystem.S._DEBUG_VISUAL_plotTileFields (new Vector2 (xs, ys), cce.v);
//			NavSystem.S.theMapAnalyzer.printOutMatrix (cce.f);
		}
	}

	IEnumerator UnitVelocityUpdate() {
		while (true) {			
			foreach (CM_Unit_Goal_Group cmugg in unitGoalGroups) {	
				cmugg.setUnitVelocities ();
			}
			yield return new WaitForSeconds (UnitVelocityUpdateTime);
		}
	}

	// **********************************************************************************************************
	//			backup helper functions
	// **********************************************************************************************************
	List<Rect> getCurrentMoveTarget() {
		List<Rect> newGoals = new List<Rect> ();
		newGoals.Add (goal);
		return newGoals;
	}

	void setAllGoalLists() {
		for( int i=0; i<unitGoalGroups.Count; i++){
			CM_Unit_Goal_Group cmugg = unitGoalGroups[i];
			cmugg.setGoalList (getCurrentMoveTarget ());
			unitGoalGroups [i] = cmugg;
		}
	}

	Vector2 getUnitAveragePosition() {
		Vector2 ave = Vector2.zero;
		foreach(Unit u in myUnits) {
			ave += u.getPosition ();
		}
		if (myUnits.Count > 0)
			ave /= myUnits.Count;
		return ave;
	}

	void setCurrentMoveTarget(Vector2 p) {
		nextGoal = p;
		goal = new Rect (p.x-basicGoalDimension/2, p.y-basicGoalDimension/2, basicGoalDimension, basicGoalDimension);
		goalList = new List<Rect> ();
		goalList.Add (goal);
	}
}
