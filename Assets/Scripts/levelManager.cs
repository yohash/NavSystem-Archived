using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class levelManager : MonoBehaviour {

	public Camera camera;

	public Vector2 start, goal;

	public GameObject theAStarPath;
	GameObject pathMesh;

	public NavSystem _NavSystem;

	public CombatManager CM;

	public Unit testTank;

	public List<Vector2> AStarCalculatedPath;
	public List<Vector3> pathLocations;

	int _mapX, _mapZ;

	public struct LocationTest {
		public int x;
		public int y;
	}

	void Start () {
//		LocationTest lt1 = new LocationTest ();
//		lt1.x = 0;
//		lt1.y = 0;
//
//		Location l1 = new Location (0, 0);
//		Location l2 = new Location(0, 0);
//		Debug.Log ("hey heyh hey what's up");
//		Debug.Log (l1.Equals (lt1));


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
		if (Input.GetKey (KeyCode.Mouse1)) {
			Vector3 cmpos = CM.getPosition ();
			start = new Vector2 (cmpos.x, cmpos.z);
			setGoal ();
			plotAStarPath ();
			CM.setCurrentPath (AStarCalculatedPath);
		}
	}

	void delayedCommands () {
		CM.addUnitToCombatManagerGroup (testTank);
		NavSystem.S.addCCUnitToDynamicFields (testTank);
//		CCEikonalSolver cce = _NavSystem._DEBUG_EIKONAL_computeCCVelocityField (sol, locs);
//		_NavSystem._DEBUG_VISUAL_boxAStarNodes ();
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


	Vector3 setAStarLocation() {
		RaycastHit hit;
		Ray ray = camera.ScreenPointToRay(Input.mousePosition);

		int mask = 1 << 8;
		if (Physics.Raycast(ray, out hit, Mathf.Infinity, mask)) {
			return new Vector2(hit.point.x,hit.point.z);
		}
		return Vector2.zero;
	}

	void plotAStarPath() {
		pathLocations = NavSystem.S.plotAStarOptimalPath (start, goal);
		pathLocations.Reverse();
		AStarCalculatedPath = new List<Vector2> ();
		if (pathLocations.Count > 0) {
			foreach (Vector3 v in pathLocations) {
				Vector2 vt = new Vector2 (v.x, v.z);
				AStarCalculatedPath.Add (vt);
			}

			pathMesh.GetComponent<meshLineGenerator> ().setLinePoints (pathLocations.ToArray (), new Vector3[pathLocations.Count], 0.5f);
			pathMesh.GetComponent<meshLineGenerator> ().generateMesh ();
		}
	}
}
