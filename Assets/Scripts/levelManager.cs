using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class levelManager : MonoBehaviour {

	public Camera camera;

	public Vector2 start, goal;

	public GameObject theAStarPath;
	GameObject pathMesh;

	public NavSystem _NavSystem;

	int _mapX, _mapZ;

	void Start () {
		camera = GetComponent<Camera> ();
		pathMesh = Instantiate (theAStarPath) as GameObject;

		_NavSystem = NavSystem.S;
		_NavSystem.Initialize_NavSystem ();

		_mapX = _NavSystem.getMapWidthX();
		_mapZ = _NavSystem.getMapLengthZ();

		Invoke ("delayedCommands", 2f);
	}


	void Update() {
		if (Input.GetKey (KeyCode.Mouse0)) {
			start = setAStarLocation ();
		}
		if (Input.GetKey (KeyCode.Mouse1)) {
			goal = setAStarLocation ();
			plotAStarPath ();
		}
	}


	void delayedCommands () {
		Rect sol = new Rect (0, 0, _mapX, _mapZ);
		Location l = new Location (35, 35);
		List<Location> locs = new List<Location> ();
		locs.Add (l);

		CCEikonalSolver cce = _NavSystem._DEBUG_EIKONAL_computeCCVelocityField (sol, locs);
		_NavSystem._DEBUG_VISUAL_boxAStarNodes ();
		_NavSystem._DEBUG_VISUAL_plotTileFields (cce.Phi);
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
		List<Vector3> pathLocations = NavSystem.S.plotAStarOptimalPath (start, goal);

		if (pathLocations.Count > 0) {
			pathMesh.GetComponent<meshLineGenerator> ().setLinePoints (pathLocations.ToArray (), new Vector3[pathLocations.Count], 0.5f);
			pathMesh.GetComponent<meshLineGenerator> ().generateMesh ();
		}
	}

}
