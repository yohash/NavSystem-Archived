using UnityEngine;
using System.Collections;

public class CC_Unit {
	private Unit _myUnit;

	// private variables
	private Vector2 _CC_Unit_velocity;
	private Vector2 _CC_Unit_position;

	// getters and setters
	public Vector2 getVelocity() {return _CC_Unit_velocity;}
	public Vector2 getPosition() {return _CC_Unit_position;}

	public void setVelocity(Vector2 v) {_CC_Unit_velocity = v;}
	public void setPostiion(Vector2 v) {_CC_Unit_position = v;}

	public CC_Unit(Vector2 unitVelocity, Vector2 unitPosition, Unit u) {
		_CC_Unit_velocity = unitVelocity;
		_CC_Unit_position = unitPosition;
		_myUnit = u;
	}

	public void updatePhysics() {
		_CC_Unit_position = _myUnit.getPosition();
		_CC_Unit_velocity = _myUnit.getVelocity();
	}
}
