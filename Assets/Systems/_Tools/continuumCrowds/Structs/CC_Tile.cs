using UnityEngine;
using System.Collections;

// the CC_Tile struct is specficially built to hold
// the field values for Continuum Crowd that are
// common amongst all units.

public class CC_Tile {
	// tile dimension (all tiles are square)
	int dim;

	// testing this -- only update tiles with 
	// units moving around in them
	public bool UPDATE_TILE;

	// might need to store this
	public Location myLoc;

	// hard fields
	public float[,] g;
	public Vector2[,] dh;

	// the Continuum Crowds Dynamic Global fields
	public float[,] rho;				// density field
	public Vector2[,] vAve;				// average velocity field
	public float[,] gP;					// predictive discomfort
	public Vector4[,] f;				// speed field, data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	public Vector4[,] C;				// cost field, data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)

	public CC_Tile(int d, Location l) {
		dim = d;
		myLoc = l;

		g = new float[dim, dim];
		dh = new Vector2[dim, dim];

		rho = new float[dim, dim];
		gP = new float[dim, dim];
		vAve = new Vector2[dim, dim];
		f = new Vector4[dim, dim];
		C = new Vector4[dim, dim];

		UPDATE_TILE = false;
	}

	public void resetTile() {
		for (int i = 0; i < dim; i++) {
			for (int k = 0; k < dim; k++) {
				rho [i, k] = 0;
				gP [i, k] = 0;
				vAve [i, k] = Vector2.zero;
				f [i, k] = Vector4.zero;
				C [i, k] = Vector4.zero;
			}
		}

		UPDATE_TILE = false;
	}

	// **************************************************************
	//  		WRITE data
	// (I tried making a generic function that would read/write to the
	// proper matrix based on the input Type, but it was hard, and I
	// wasnt seeing success. Thus, this 'brute force' approach)
	// **************************************************************
	public void writeData_g(int xTile, int yTile, float f) {
		g [xTile, yTile] = f;
	}
	public void writeData_dh(int xTile, int yTile, Vector2 v) {
		dh [xTile, yTile] = v;
	}
	public void writeData_rho(int xTile, int yTile, float f) {
		rho [xTile, yTile] = f;
	}
	public void writeData_gP(int xTile, int yTile, float f) {
		gP [xTile, yTile] = f;
	}
	public void writeData_vAve(int xTile, int yTile, Vector2 f) {
		vAve [xTile, yTile] = f;
	}
	public void writeData_f(int xTile, int yTile, Vector4 v) {
		f [xTile, yTile] = v;
	}
	public void writeData_C(int xTile, int yTile, Vector4 v) {
		C [xTile, yTile] = v;
	}

	// **************************************************************
	//  		READ data
	// **************************************************************
	public float readData_g(int xTile, int yTile) {
		return g [xTile, yTile];
	}
	public Vector2 readData_dh(int xTile, int yTile) {
		return dh [xTile, yTile];
	}
	public float readData_rho(int xTile, int yTile) {
		return rho [xTile, yTile];
	}
	public float readData_gP(int xTile, int yTile) {
		return gP [xTile, yTile];
	}
	public Vector2 readData_vAve(int xTile, int yTile) {
		return vAve [xTile, yTile];
	}
	public Vector4 readData_f(int xTile, int yTile) {
		return f [xTile, yTile];
	}
	public Vector4 readData_C(int xTile, int yTile) {
		return C [xTile, yTile];
	}
}
