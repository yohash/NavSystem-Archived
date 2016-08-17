using UnityEngine;
using System.Collections;

public struct CC_Unit {
	// private variables
	private Vector2 _CC_Unit_velocity;
	private Vector2 _CC_Unit_position;

	// getters and setters
	public Vector2 getVelocity() {return _CC_Unit_velocity;}
	public Vector2 getPosition() {return _CC_Unit_position;}

	public void setVelocity(Vector2 v) {_CC_Unit_velocity = v;}
	public void setPostiion(Vector2 v) {_CC_Unit_position = v;}
}
