using UnityEngine;
using System.Collections;

public class CC_Unit {
	private Unit _myUnit;

	// private variables
	private Vector2 _CC_Unit_velocity;
	private Vector2[] _CC_Unit_positions;

	// getters and setters
	public Vector2 getVelocity() {return _CC_Unit_velocity;}
	public Vector2[] getPositions() {return _CC_Unit_positions;}


	public CC_Unit(Unit u) {
		_myUnit = u;
		_CC_Unit_positions = new Vector2[(u.getLength () + 1) * (u.getWidth () + 1)];
	}

	// the continuumCrowds code considers units lower-left location (similar to a rect)
	// currently, my units are CENTERED on their transform, so we subtract half their size
	public void updatePhysics() {
		_CC_Unit_positions = _myUnit.getUnitEquivalentPositions();
		_CC_Unit_velocity = _myUnit.getVelocity();
	}
}
