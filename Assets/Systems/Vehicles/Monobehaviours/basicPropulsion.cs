using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class basicPropulsion : MonoBehaviour {

	public Rigidbody locomotionRB;

	Transform tr, treads;

	public Renderer breaklight_L_rend, breaklight_R_rend;
	Material mat_L, mat_R;
	Color col;

	public bool isBreaking, isReversing;

	public Vector3 currentAppliedForce;

	public float maxSpeed = 5f;

	public float mass;
	public float linearAccel = 20f;
	Vector3 acceleration;

	public float linearForce;

	public Vector3 currentVelocity;
	public float currentSpeedSq, newSpeedSq;

	public float breakingAccel = 10f;
	public float breakingForce;


	// this value is given to the propulsion system from the unit itself
	// the value will be given to the Unit by its Combat Manager, after it calls
	// for an Eikonal solution
	public Vector2 desiredVelocity;
	public Vector3 currentForward;
	public Vector3 newForwardAfterRote;

	public float maxTurningDegreesPerSecond;
	public float maxTurningRadiansPerSecond;

	public void setVelocity(Vector2 v) {
		desiredVelocity = v;
	}

	void Awake () {
		tr = transform;
		treads = locomotionRB.transform;

		mat_L = breaklight_L_rend.material;
		mat_R = breaklight_R_rend.material;
		col = mat_L.color;            // get the color
		mat_L.EnableKeyword("_EMISSION");     
		mat_R.EnableKeyword("_EMISSION");             

		mass = locomotionRB.mass;
		linearForce = linearAccel * mass;
		breakingForce = breakingAccel * mass;

		maxTurningRadiansPerSecond = maxTurningDegreesPerSecond * Mathf.PI / 180f;
	}

	void FixedUpdate() {
		manageUnitMovements (desiredVelocity);
		currentVelocity = locomotionRB.velocity;
	}



	void turnOnBreakLights() {
		isBreaking = true;
		mat_L.SetColor("_EmissionColor", col * 3);  
		mat_R.SetColor("_EmissionColor", col * 3);    
	}

	void turnOffBreakLights() {
		isBreaking = false;
		mat_L.SetColor("_EmissionColor", Color.black);  
		mat_R.SetColor("_EmissionColor", Color.black);    
	}


	void manageUnitMovements(Vector2 newVelocity) {
		currentSpeedSq = locomotionRB.velocity.sqrMagnitude;

		currentForward = tr.TransformDirection (0, 0, 1f);
		currentForward.y = 0f;

		newSpeedSq = newVelocity.sqrMagnitude;
		Vector3 newVel3 = new Vector3 (newVelocity.x, 0f, newVelocity.y);

		// ensure scaledVelocity is not greater than maxSpeed
		if (newSpeedSq > (maxSpeed*maxSpeed)) {newVelocity = newVelocity.normalized * maxSpeed;}

		// have car speed up/step on breaks depending on relative speed
		if (newSpeedSq == 0) {
			currentAppliedForce = Vector3.zero;
			if (!isBreaking)
				turnOnBreakLights ();
		} else if (currentSpeedSq < newSpeedSq * (0.9f)) {
			// apply ignition
//			if ((newVelocity.x * currentForward.x + newVelocity.y * currentForward.z) < 0) {
//				currentAppliedForce = -currentForward * breakingForce;
//				if (!isBreaking)
//					turnOnBreakLights ();
//			} 
			currentAppliedForce = currentForward * linearForce;
			if (isBreaking)
				turnOffBreakLights ();
			
			locomotionRB.AddForce (currentAppliedForce, ForceMode.Force);
		} else if (currentSpeedSq > newSpeedSq * (1.21f) && (newVelocity.x * currentVelocity.x + newVelocity.y * currentVelocity.z) > 0) {
			// apply breaking force
			currentAppliedForce = -currentForward * breakingForce;
			locomotionRB.AddForce (currentAppliedForce, ForceMode.Force);
			if (!isBreaking)
				turnOnBreakLights ();
		}

		// have car turn based on turning radius
		if (newSpeedSq > 0) {
			if (currentSpeedSq < 1) {
				newForwardAfterRote = Vector3.RotateTowards (currentForward, newVel3, maxTurningRadiansPerSecond * Time.deltaTime * currentSpeedSq, 0f);
			} else {
				newForwardAfterRote = Vector3.RotateTowards (currentForward, newVel3, maxTurningRadiansPerSecond * Time.deltaTime / (currentSpeedSq/4f), 0f);
			}
			locomotionRB.MoveRotation (locomotionRB.rotation * Quaternion.FromToRotation(currentForward,newForwardAfterRote));
		}
	}




	public Vector3 getCurrentVelocity() {
		return locomotionRB.velocity;
	}
}
