using UnityEngine;
using System;
using System.Collections;

public class Unit : MonoBehaviour {
	// by reference (width,length) in (x,z), the unit is 
	// oriented its own local z-axis. 
	// since Vector3.forward = (0,0,1), this will make propulsion easier
	// width, length
	int sizeX, sizeZ;
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

	public Vector2 DEBUG_TILE_LOC;


	void Awake () {
		tr = transform;
		propulsionSystem = GetComponent<basicPropulsion> ();
	}

	void Start () {

	}

	void Update () {
		_position = new Vector2 (tr.position.x, tr.position.z);

		_position += _velocity * Time.deltaTime;
		tr.position = new Vector3(_position.x, NavSystem.S.getHeightAtPoint(_position.x, _position.y),_position.y);


		Location l = new Location ( (int) Math.Floor (((double)_position.x) / ((double)NavSystem.S.tileSize)), 
			(int) Math.Floor (((double)_position.y) / ((double)NavSystem.S.tileSize)));
		DEBUG_TILE_LOC = new Vector2 (l.x, l.y);
	}

	void OnEnable() {

	}

	void OnDisable() {

	}


	public void setDesiredVelocity(Vector2 v) {
		if (v.sqrMagnitude > maxSpeed * maxSpeed) {
			v = v.normalized * maxSpeed;
		}
		_velocity = v;
	}
}
