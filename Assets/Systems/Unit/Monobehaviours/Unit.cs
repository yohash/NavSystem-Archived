using UnityEngine;
using System;
using System.Collections;

public class Unit : MonoBehaviour {
	// by reference (width,length) in (x,z), the unit is 
	// oriented its own local z-axis. 
	// since Vector3.forward = (0,0,1), this will make propulsion easier
	// width, length
	public int sizeX = 1, sizeZ = 1;

	public int getWidth() {
		return sizeX;
	}
	public int getLength() {
		return sizeZ;
	}
	public Vector2 getSize() {
		return (new Vector2 (sizeX, sizeZ));
	}

	public float maxSpeed = 5f;

	public Vector2 _velocity;
	public Vector2 _position;
	public Vector2 getVelocity() {return _velocity;}
	public Vector2 getPosition() {return _position;}

	basicPropulsion propulsionSystem;

	Transform tr;

	void Awake () {
		tr = transform;
		propulsionSystem = GetComponent<basicPropulsion> ();
	}

	void Start () {
	}

	void Update () {
		// update our current position for equivalent CC_Unit update
		_position = new Vector2 (tr.position.x, tr.position.z);

		// update our current velocity from the rigidbody
		Vector3 v3 = propulsionSystem.getCurrentVelocity();
		_velocity = new Vector2 (v3.x, v3.z);
	}

	void OnEnable() {

	}

	void OnDisable() {

	}

	// this function is fed a velocity value normalized from 0-1 (in magnitude)
	// it scales it by its current maxspeed to yield the desired velocity
	public void setDesiredVelocity(Vector2 v) {
		v *= maxSpeed;
		// check just in case, make sure we dont exceed maxSpeed
		if (v.sqrMagnitude > maxSpeed * maxSpeed) {
			v = v.normalized * maxSpeed;
		}
		
		propulsionSystem.setVelocity (v);
	}


	// *****************************************************************************************
	//			UNIT EQUIVALENT POSITION - This is for pathfinding. It takes a 
	//				'footprint' of the unit (+some small padding), rotates it, and 
	//				send the footprint to the navigation system
	// *****************************************************************************************


	public Vector2[] getUnitEquivalentPositions() {
		int numCols = sizeX + 1;
		int numRows = sizeZ + 1;

		Vector2[] CC_Unit_Locs = new Vector2[numRows*numCols];

		// arrange the grid
		for (int i=0; i<numCols; i++) {
			for (int k=0; k<numRows; k++) {
				CC_Unit_Locs [i * numRows + k] = new Vector2 (i - ((float)sizeX) / 2f, k - ((float)sizeZ) / 2f);
			}
		}
		Vector3 currentForward = tr.TransformDirection (0, 0, 1f);
		currentForward.y = 0f;

		float roteAngle = Vector3.Angle (new Vector3(0f,0f,1f), currentForward);
		if (currentForward.x < 0) {
			roteAngle = 360f - roteAngle;
		}
		roteAngle *= -1;  // I calculate the vector to rotate clockwise about (0,0), when ACTUALLY
		// rotation matrices rotate vectors COUNTER clockwise (in direction of +phase)
		// so I gotta *=(-1) this guy
		// then we rotate the vector
		Vector2 temp;
		for (int i=0; i<CC_Unit_Locs.GetLength(0); i++) {
			temp = CC_Unit_Locs[i];
			temp.x = Mathf.Cos (roteAngle * Mathf.Deg2Rad) * CC_Unit_Locs[i].x - Mathf.Sin (roteAngle * Mathf.Deg2Rad) * CC_Unit_Locs[i].y;
			temp.y = Mathf.Sin (roteAngle * Mathf.Deg2Rad) * CC_Unit_Locs[i].x + Mathf.Cos (roteAngle * Mathf.Deg2Rad) * CC_Unit_Locs[i].y;
			temp.x = temp.x + _position.x - 0.5f;
			temp.y = temp.y + _position.y - 0.5f;
			CC_Unit_Locs[i] = temp;
		}
		return CC_Unit_Locs;
	}
}
