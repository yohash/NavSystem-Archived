using UnityEngine;
using System.Collections;

public static class CCvals {
	// parameters for the respective equations 
	public static float rho_sc = 1.2f;					// scalar for density to splat onto discomfort map

	public static float gP_predictiveSeconds = 4f;		// how far into future we predict the path
	public static float gP_weight = 2f;					// scalar for predictive discomfort to splat onto discomfort map

	public static float f_slopeMax = 1f;				// correlates roughly to 30-degree incline
	public static float f_slopeMin = 0f;				// for a min slope, nothing else makes sense...

	public static float f_rhoMax = 0.6f;				// everything above this must be clamped to 'unpassable' discomfort map
	public static float f_rhoMin = 0.2f;

	public static float f_speedMax = 10f;				// will this vary by unit???
	public static float f_speedMin = 0.01f;				// set to some positive number to clamp flow speed

	public static float C_alpha = 2f;					// speed field weight
	public static float C_beta = 8f;					// time weight
	public static float C_gamma = 2f;					// discomfort weight
}
