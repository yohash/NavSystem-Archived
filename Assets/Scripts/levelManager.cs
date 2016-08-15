using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class levelManager : MonoBehaviour {

	public Camera camera;

	public Vector2 start, goal;

	public GameObject theAStarPath;
	GameObject pathMesh;

	void Start () {
		camera = GetComponent<Camera> ();
		pathMesh = Instantiate (theAStarPath) as GameObject;
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

		pathMesh.GetComponent<meshLineGenerator>().setLinePoints(pathLocations.ToArray(), new Vector3[pathLocations.Count],0.5f);
		pathMesh.GetComponent<meshLineGenerator>().generateMesh();
	}

}
