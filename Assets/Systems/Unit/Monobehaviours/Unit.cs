using UnityEngine;
using System.Collections;

public class Unit : MonoBehaviour {
	// by reference (width,length) in (x,z), the unit is 
	// oriented its own local z-axis. 
	// since Vector3.forward = (0,0,1), this will make propulsion easier
	// width, length
	int sizeX, sizeZ;

	private Vector2 _velocity;
	private Vector2 _position;
	public Vector2 getVelocity() {return _velocity;}

	basicPropulsion propulsionSystem;

	Transform tr;


	void Awake () {
		tr = transform;
		propulsionSystem = GetComponent<basicPropulsion> ();
	}

	void Start () {

	}

	void OnEnable() {

	}

	void OnDisable() {

	}
}
