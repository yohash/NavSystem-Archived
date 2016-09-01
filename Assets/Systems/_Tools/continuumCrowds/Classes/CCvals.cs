using UnityEngine;
using System.Collections;

public static class CCvals {
	// parameters for the respective equations 
	public static float rho_sc = 1.2f;					// scalar for density to splat onto discomfort map

	public static float gP_predictiveSeconds = 8f;		// how far into future we predict the path
	public static float gP_weight = 1f;					// scalar for predictive discomfort to splat onto discomfort map

	public static float f_slopeMax = 1f;				// everything above this must be clamped to 'unpassable' discomfort map
	public static float f_slopeMin = 0f;				// for a min slope, nothing else makes sense...

	public static float f_rhoMax = 0.6f;				
	public static float f_rhoMin = 0.2f;

	public static float f_speedMin = 0.01f;				// set to some small positive number to clamp flow speed
	public static float f_speedMax = 40f;				// set this to 1 to automatically just receive normalized 'direction'
														// then, we simply scale by the units particular maxSpeed

	public static float C_alpha = 10f;					// speed field weight
	public static float C_beta = 1f;					// time weight
	public static float C_gamma = 1f;					// discomfort weight
}
