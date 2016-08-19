using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CombatManager : MonoBehaviour {
	
	public List<Unit> myUnits;

	public Dictionary<Unit, float> unitPriorities;
	public Dictionary<Unit, float> enemyPriorities;

	public List<CC_Unit> myUnits_CCU;
	public List<CM_Unit_Goal_Groups> unitGoalGroups;

	// until paths are implemented, use this
	public List<Vector2> myCurrentPath;
	public Queue<Vector2> currentPath;

	public Vector2 nextPoint;
	public float minDistForNewPoint;
	float minDistToStop = 0.5f;
	float minDistForNewPointSq;

	public float floatHeight;

	public float CMUpdateFPS;
	public float CMUpdateTime;

	float baseSolutionSpaceBuffer = 10f;
	float basicGoalDimension = 4f;


	public Rect goal;
	List<Rect> goalList;

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
		unitGoalGroups = new List<CM_Unit_Goal_Groups> ();
		myUnits_CCU = new List<CC_Unit> ();

		currentPath = new Queue<Vector2> ();
		goalList = new List<Rect> ();

		minDistForNewPointSq = minDistForNewPoint * minDistForNewPoint;
		CMUpdateTime = 1 / CMUpdateFPS;
		StartCoroutine ("CMUpdateLoop");
	}


	IEnumerator CMUpdateLoop() {
		while (true) {
			if (nextPoint != Vector2.zero) {
				// check to see how far from the next point we are
				Vector2 currentLocV2 = new Vector2(tr.position.x, tr.position.z);
				float distSq = (currentLocV2 - nextPoint).sqrMagnitude;

				if (distSq > minDistForNewPointSq) {
					// if (further than DIST)		->		check last update time (if longer than T) 
					// foreach (CM_UGG) 			->		get new v field
					// foreach (unit in CM_UGG) 	->		assign new v
					foreach (CM_Unit_Goal_Groups cmugg in unitGoalGroups) {						
						if (cmugg.unitGoalGroupNeedsUpdate ()) {

							cmugg.reBoundUnitsAndGoals (baseSolutionSpaceBuffer);

//							CCEikonalSolver cce = NavSystem.S._DEBUG_EIKONAL_computeCCVelocityField (cmugg.unitGoalSolutionSpace, cmugg.goals);
//							int xs = Mathf.FloorToInt (cmugg.unitGoalSolutionSpace.x);
//							int ys = Mathf.FloorToInt (cmugg.unitGoalSolutionSpace.y);
//							NavSystem.S._DEBUG_VISUAL_plotTileFields (new Vector2 (xs, ys), cce.Phi);

							Vector2[,] vField = NavSystem.S.computeCCVelocityField (cmugg.unitGoalSolutionSpace, cmugg.goals);
							cmugg.setVelocityField (vField);
							cmugg.setUnitVelocities ();
						}
					}

				} else  {
					// else (closer than DIST to NEXTPOINT) 		-> 		get next point
					//	if (no next point && DIST<smallerDIST) 		->		nextPoint = ZERO	
					if (currentPath.Count > 0) {
						setCurrentMoveTarget (currentPath.Dequeue ());
						setAllGoalLists ();
						foreach (CM_Unit_Goal_Groups cmugg in unitGoalGroups) {			
							cmugg.setUnitVelocities ();
						}
					} else {
						setCurrentMoveTarget (Vector2.zero);
					}
				}
			}
			yield return new WaitForSeconds (CMUpdateTime);
		}
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

		setCurrentMoveTarget (currentPath.Dequeue());
	}

	public void setCurrentMoveTarget(Vector2 p) {
		nextPoint = p;
		goal = new Rect (p.x-basicGoalDimension/2, p.y-basicGoalDimension/2, basicGoalDimension/2, basicGoalDimension/2);
		goalList = new List<Rect> ();
		goalList.Add (goal);
	}

	public void addUnitToCombatManagerGroup(Unit u) {
		myUnits.Add (u);

		if (unitGoalGroups.Count == 0) {
			CM_Unit_Goal_Groups cmugg = new CM_Unit_Goal_Groups (myUnits, getCurrentMoveTarget ());
			unitGoalGroups.Add (cmugg);
		} else {
			unitGoalGroups [0].addUnitToGroup (u);
		}
	}

	public void setAllGoalLists() {
		for( int i=0; i<unitGoalGroups.Count; i++){
			CM_Unit_Goal_Groups cmugg = unitGoalGroups[i];
			cmugg.setGoalList (getCurrentMoveTarget ());
			unitGoalGroups [i] = cmugg;
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

	Vector2 getUnitAveragePosition() {
		Vector2 ave = Vector2.zero;
		foreach(Unit u in myUnits) {
			ave += u.getPosition ();
		}
		if (myUnits.Count > 0)
			ave /= myUnits.Count;
		return ave;
	}
}
