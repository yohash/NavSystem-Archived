using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Threading;
using Foundation.Tasks;


public class levelManager : MonoBehaviour {

	public Camera camera;

	public Vector2 start, goal;

	public GameObject theAStarPath;
	GameObject pathMesh;

	public NavSystem _NavSystem;

	public CombatManager CM;
	public CombatManager CM2;

	public List<Unit> testTanks;
	public List<Unit> testTanks2;

	public List<Vector2> optimalPath;
	public List<Vector3> pathLocations;

	public float minDistSQForAStar = 16f;

	int _mapX, _mapZ;

	// cache one instance of this for creating searches
	AStarSearch astar;

	// ****************************************************************************************************
	//		   	INITIATION
	// ****************************************************************************************************
	void Start () {
		camera = GetComponent<Camera> ();
		pathMesh = Instantiate (theAStarPath) as GameObject;

		pathLocations = new List<Vector3> ();

		_NavSystem = NavSystem.S;
		_NavSystem.Initialize_NavSystem ();

		_mapX = _NavSystem.getMapWidthX();
		_mapZ = _NavSystem.getMapLengthZ();

		Invoke ("delayedCommands", 0.5f);
	}

	void Update() {
		if (Input.GetKeyDown (KeyCode.Mouse1)) {
			Vector3 cmpos = CM.getPosition ();
			start = new Vector2 (cmpos.x, cmpos.z);
			setGoal ();
			plotAStarPath (CM);
		}
		if (Input.GetKeyDown (KeyCode.Mouse0)) {
			Vector3 cmpos = CM2.getPosition ();
			start = new Vector2 (cmpos.x, cmpos.z);
			setGoal ();
			plotAStarPath (CM2);
		}
	}

	void delayedCommands () {
		foreach (Unit u in testTanks) {
			CM.addUnitToCombatManagerGroup (u);
			NavSystem.S.addCCUnitToDynamicFields (u);
		}
		foreach (Unit u in testTanks2) {
			CM2.addUnitToCombatManagerGroup (u);
			NavSystem.S.addCCUnitToDynamicFields (u);
		}
		_NavSystem._DEBUG_VISUAL_boxAStarNodes ();
//		_NavSystem._DEBUG_VISUAL_plotNodeCenterPoints ();
//		_NavSystem._DEBUG_VISUAL_plotNodeNeighbors ();
//		_NavSystem._DEBUG_VISUAL_plotTileFields ();
	}

	void setStart () {
		start = setAStarLocation ();
	}
	void setGoal () {
		goal = setAStarLocation ();
	}

	// ****************************************************************************************************
	//		MULTI-THREADED FUNCTIONS, THANKS TO UNITY TASKS
	// ****************************************************************************************************
	IEnumerator _plotAStar(Vector3 s, Vector3 g, CombatManager cmt) {		
		astar = new AStarSearch (NavSystem.S.getAStarGrid (),s,g);
		astar.initiateSearch ();

		yield return StartCoroutine (astar.WaitFor ());

		plotTheRoute (cmt);
	}
	// ****************************************************************************************************

	Vector3 setAStarLocation() {
		RaycastHit hit;
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);

		int mask = 1 << 8;
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask)) {
			return new Vector2(hit.point.x,hit.point.z);
		}
		return Vector2.zero;
	}

	void plotAStarPath(CombatManager cmt) {
		StartCoroutine (_plotAStar (start, goal, cmt));
	}

	void plotTheRoute(CombatManager cmt) {
		float dSQ = (start-goal).sqrMagnitude;

		optimalPath.Clear();
		if (dSQ > minDistSQForAStar) {
			optimalPath.AddRange (astar.getAStarOptimalPath ());
		}
		optimalPath.Add (goal);

		pathLocations = new List<Vector3> ();

		if (optimalPath.Count > 0) {
			pathLocations.Add (new Vector3 (start.x, NavSystem.S.getHeightAtPoint(start.x,start.y),start.y));
			foreach (Vector2 vt in optimalPath) {
				pathLocations.Add (new Vector3(vt.x, NavSystem.S.getHeightAtPoint(vt.x,vt.y), vt.y));
			}
			pathMesh.GetComponent<meshLineGenerator> ().setLinePoints (pathLocations.ToArray (), new Vector3[pathLocations.Count], 0.5f);
			pathMesh.GetComponent<meshLineGenerator> ().generateMesh ();
		}
		// set the CM on the newly charted path
		cmt.setCurrentPath (optimalPath);
	}
}
