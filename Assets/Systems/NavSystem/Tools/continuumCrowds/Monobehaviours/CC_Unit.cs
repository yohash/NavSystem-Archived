using UnityEngine;
using System.Collections;

public class CC_Unit : MonoBehaviour {
	// a CC_Unit is ALWAYS localized to a portion of the larger map
	// and has this localization instantiated on a continuum crowds call

	// private variables
	public float _CC_Unit_maxSpeed = 5f;
	public Vector2 _CC_Unit_velocity;
	public Vector2 _CC_Unit_position;
	public Rect _CC_Unit_localGoal;
	Vector3 tmp3;

	public Vector2 _CC_worldspace_anchor;
	public Rect _CC_worldspace_goal;

	// getters and setters
	public Vector2 getVelocity() {return _CC_Unit_velocity;}
	public Vector2 getPosition() {return _CC_Unit_position;}
	public Rect getLocalGoal() {return _CC_Unit_localGoal;}

	public void setVelocity(Vector2 v) {_CC_Unit_velocity = v;}
	public void setGoal(Rect r) {_CC_Unit_localGoal = r;}

	Transform tr;

	void Awake () {tr = transform;}
	void Start () {	}
	void OnEnable () { }

	void Update () {
		// SUPER temporary code
		if (_CC_Unit_velocity!=Vector2.zero) {
			if (_CC_Unit_velocity.SqrMagnitude() > _CC_Unit_maxSpeed*_CC_Unit_maxSpeed) {
				_CC_Unit_velocity = _CC_Unit_velocity.normalized * _CC_Unit_maxSpeed;
			}
			tmp3 = new Vector3( _CC_Unit_velocity.x, 0f, _CC_Unit_velocity.y);
		}
		tr.position += tmp3 * Time.deltaTime;

		_CC_Unit_position = new Vector2(tr.position.x, tr.position.z);
	}

	public void packageForCCSubmission(Vector2 worldSpace_anchor, Rect worldSpace_goal) {
		setGoal(new Rect(worldSpace_goal.min - worldSpace_anchor, worldSpace_goal.size));
	}
}
