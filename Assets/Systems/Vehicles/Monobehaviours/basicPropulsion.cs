using UnityEngine;
using System.Collections;


public class basicPropulsion : MonoBehaviour {

	public Rigidbody locomotion;

	Transform tr, treads;

	public Renderer breaklight_L_rend, breaklight_R_rend;
	Material mat_L, mat_R;
	Color col;

	bool isBreaking;

	public float currentAppliedForce;
	public float currentAppliedTorque;

	public float maxSpeed = 5f;

	public float mass;
	public float linearAccel = 20f;

	public float linearForce;

	public float breakingAccel = 10f;
	public float breakingForce;

	// this value is given to the propulsion system from the unit itself
	// the value will be given to the Unit by its Combat Manager, after it calls
	// for an Eikonal solution
	public Vector2 desiredVelocity;

	public float torque = 20f;

	public void setVelocity(Vector2 v) {
		desiredVelocity = v;
	}


	void Awake () {
		tr = transform;
		treads = locomotion.transform;

		mat_L = breaklight_L_rend.material;
		mat_R = breaklight_R_rend.material;
		col = mat_L.color;            // get the color
		mat_L.EnableKeyword("_EMISSION");     
		mat_R.EnableKeyword("_EMISSION");             

		mass = locomotion.mass;
		linearForce = linearAccel * mass;
		breakingForce = breakingAccel * mass;
	}

	void FixedUpdate() {
//		locomotion.AddForce(treads.forward * currentAppliedForce, ForceMode.Force);
//		locomotion.AddTorque(tr.up * currentAppliedTorque, ForceMode.Force);
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


//	Vector3 dir;
//	void manageUnitMovements(Vector3 newVelocity) {
//		// ensure scaledVelocity is not greater than maxSpeed
//		if (newVelocity.sqrMagnitude > (maxSpeed*maxSpeed)) {newVelocity = newVelocity.normalized * maxSpeed;}
//
//		// acceleration is delta-V over delta-t
//		// delta-t isnt shown here because...
//		acceleration = newVelocity - currentVelocity;
//
//		// ... v = v_0 + a*t , and a = dV/dt, and t = dt = time.deltaTime, 
//		// so they cancel out
//		currentVelocity += acceleration * accelerationModifier;
//		currentSpeed = currentVelocity.magnitude;
//
//		currentVelocity = new Vector3(currentVelocity.x,0f,currentVelocity.z);
//		tr.position += currentVelocity * Time.deltaTime;
//
//		if (steering.DEBUG_MODE) {Debug.DrawRay(tr.position+new Vector3(0,1,0),currentVelocity, Color.white,0.25f);}
//
//		// now, blur the turn between the last 5 velocity samples
//		velocitySamples.Dequeue();
//		velocitySamples.Enqueue(currentVelocity);
//
//		dir = Vector3.zero;
//		foreach(Vector3 v in velocitySamples) {
//			dir += v;
//		}
//		dir /= velocitySamples.Count;
//
//		tr.LookAt(tr.position + dir);
//	}

}
