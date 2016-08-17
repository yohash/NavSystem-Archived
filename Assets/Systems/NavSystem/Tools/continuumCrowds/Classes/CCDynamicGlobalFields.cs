using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CCDynamicGlobalFields
{
	public int tileSize = 20;

	// the meat of the CC Dynamic Global Fields computer
	private Dictionary<Location, CC_Tile> _tiles;
	private List<CC_Unit> _units;

	// map dimensions
	private int _mapX, _mapY;

	// this array of Vect2's correlates to our data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	Vector2[] DIR_ENWS = new Vector2[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

	// ******************************************************************************************
	// 				PUBLIC ACCESSORS and CONSTRUCTORS
	// ******************************************************************************************
	public CCDynamicGlobalFields ()
	{
		_tiles = new Dictionary<Location, CC_Tile> ();
		_units = new List<CC_Unit> ();
	}

	public void setTileSize (int s) {
		tileSize = s;
	}

	public bool initiateTiles (int mapX, int mapY, float[,] g, Vector2[,] dh) {
		// take map dimensions
		// if tileSize and map dimensions dont fit perfectly, drop a flag
		// otherwise, create all the tiles

		_mapX = mapX;
		_mapY = mapY;
		// make sure the map dimensions are divisible by tileSize
		if ((((float)mapX) % ((float)tileSize) != 0) || 
			(((float)mapY) % ((float)tileSize) != 0)) {
			// this should NEVER HAPPEN, so send an error if it does
			return false;
		} else {
			Location loc;

			int numTilesX = mapX / tileSize;
			int numTilesY = mapY / tileSize;

			// instantiate all our tiles
			for (int x = 0; x < numTilesX; x++) {
				for (int y = 0; y < numTilesY; y++) {
					// create a new tile based on this location
					loc = new Location (x, y);
					CC_Tile cct = new CC_Tile (tileSize, loc);
					_tiles.Add (loc, cct);
				}
			}

			// write the initial g and dh data required for computation
			// of fields: f, C
			for (int x = 0; x < _mapX; x++) {
				for (int y = 0; y < _mapY; y++) {
					writeDataToPoint_g (x, y, g [x, y]);
					writeDataToPoint_dh (x, y, dh [x, y]);
				}
			}
		}
		return true;
	}

	public void updateTiles ()
	{	// first, clear the tiles
		foreach (CC_Tile cct in _tiles.Values) {
//			if (cct.UPDATE_TILE) {
			cct.resetTile ();
//			}
		}
		// update the unit specific elements (rho, vAve, g_P)
		foreach (CC_Unit ccu in _units) {
			// (1) density field and velocity
			computeDensityField (ccu); 		
			// (2) predictive discomfort field
			applyPredictiveDiscomfort (CCvals.gP_predictiveSeconds, ccu);	
		}
		// these next values are derived from rho, vAve, and g_P, so we simply iterate
		// through the tiles and ONLY update the ones that have had their values changed
		int i = 0;
		foreach (CC_Tile cct in _tiles.Values) {
			//if (cct.UPDATE_TILE) {
			// (3) 	now that the velocity field and density fields are implemented,
			// 		divide the velocity by density to get average velocity field
			computeAverageVelocityField (cct);
			// (4)	now that the average velocity field is computed, and the density
			// 		field is in place, we calculate the speed field, f
			computeSpeedField (cct);
			// (5) 	the cost field depends only on f and g, so it can be computed in its
			//		entirety now as well
			computeCostField (cct);
			//}
		}
	}

	public void addNewCCUnit (CC_Unit ccu)
	{
		_units.Add (ccu);
	}

	public void removeCCUnit (CC_Unit ccu)
	{
		_units.Remove (ccu);
	}

	public void overwriteDiscomfortDataOnTiles (int globalX, int globalY, float[,] gm) {
		for (int xI = 0; xI < (gm.GetLength(0)); xI++) {
			for (int yI = 0; yI < (gm.GetLength(1)); yI++) {
				writeDataToPoint_g (xI + globalX, yI + globalY, gm [xI, yI]);
			}
		}
	}

	public CC_Map_Package buildCCMapPackage (Rect r) {
		float[,] gt;
		Vector4[,] ft, Ct;

		int xs = Mathf.FloorToInt(r.x);
		int ys = Mathf.FloorToInt(r.y);

		int xf = Mathf.CeilToInt (r.x + r.width);
		int yf = Mathf.CeilToInt (r.y + r.height);

		if (xs < 0)
			xs = 0;
		if (xf > _mapX)
			xf = _mapX;
		if (ys < 0)
			ys = 0;
		if (yf > _mapY)
			yf = _mapY;

		int xdim = xf - xs;
		int ydim = yf - ys;

		gt = new float[xdim, ydim];
		ft = new Vector4[xdim, ydim];
		Ct = new Vector4[xdim, ydim];

		for (int xI = 0; xI < xdim; xI++) {
			for (int yI = 0; yI < ydim; yI++) {
				gt [xI, yI] = readDataFromPoint_g (xs + xI, ys + yI);
				ft [xI, yI] = readDataFromPoint_f (xs + xI, ys + yI);
				Ct [xI, yI] = readDataFromPoint_C (xs + xI, ys + yI);
			}
		}

		CC_Map_Package map = new CC_Map_Package (gt, ft, Ct);
		return map;
	}
	// ******************************************************************************************
	// 							FIELD SOLVING FUNCTIONS
	// ******************************************************************************************
	private void computeDensityField (CC_Unit cc_u)
	{
		Vector2 cc_u_pos = cc_u.getPosition ();

		int xInd = Mathf.FloorToInt (cc_u_pos.x);
		int yInd = Mathf.FloorToInt (cc_u_pos.y);

		float[,] rho = linear1stOrderSplat (xInd, yInd, CCvals.rho_sc);

		if(isPointValid(xInd,yInd)) 	writeDataToPoint_rho (xInd, yInd, rho [0, 0]);
		if(isPointValid(xInd+1,yInd)) 	writeDataToPoint_rho (xInd + 1, yInd, rho [1, 0]);
		if(isPointValid(xInd,yInd+1)) 	writeDataToPoint_rho (xInd, yInd + 1, rho [0, 1]);
		if(isPointValid(xInd+1,yInd+1)) writeDataToPoint_rho (xInd + 1, yInd + 1, rho [1, 1]);

		computeVelocityFieldPoint (xInd, yInd, cc_u.getVelocity ());
		computeVelocityFieldPoint (xInd + 1, yInd, cc_u.getVelocity ());
		computeVelocityFieldPoint (xInd, yInd + 1, cc_u.getVelocity ());
		computeVelocityFieldPoint (xInd + 1, yInd + 1, cc_u.getVelocity ());
	}

	private void computeVelocityFieldPoint (int x, int y, Vector2 v)
	{
		Vector2 vAve = readDataFromPoint_vAve (x, y);
		if (isPointValid (x, y)) {
			vAve += v * readDataFromPoint_rho (x, y);
		}
		if(isPointValid(x,y)) writeDataToPoint_vAve (x, y, vAve);
	}

	private void applyPredictiveDiscomfort (float numSec, CC_Unit cc_u)
	{
		Vector2 newLoc;
		float sc;

		Vector2 xprime = cc_u.getPosition () + cc_u.getVelocity () * numSec;
		float vfMag = Vector2.Distance (cc_u.getPosition (), xprime);

		for (int i = 1; i < vfMag; i++) {
			newLoc = Vector2.MoveTowards (cc_u.getPosition (), xprime, i);

			sc = (vfMag - i) / vfMag;				// inverse scale
			float[,] gP = linear1stOrderSplat (newLoc, sc * CCvals.gP_weight);

			int xInd = Mathf.FloorToInt (newLoc.x);
			int yInd = Mathf.FloorToInt (newLoc.y);

			if(isPointValid(xInd,yInd)) 	writeDataToPoint_gP (xInd, yInd,  gP [0, 0]);
			if(isPointValid(xInd+1,yInd))	writeDataToPoint_gP (xInd + 1, yInd,  gP [1, 0]);
			if(isPointValid(xInd,yInd+1)) 	writeDataToPoint_gP (xInd, yInd + 1,  gP [0, 1]);
			if(isPointValid(xInd+1,yInd+1)) writeDataToPoint_gP (xInd + 1, yInd + 1,  gP [1, 1]);
		}
	}

	// average velocity fields will just iterate over each tile, since information
	// doesnt 'bleed' into or out from nearby tiles
	private void computeAverageVelocityField (CC_Tile cct)
	{
		for (int n = 0; n < tileSize; n++) {
			for (int m = 0; m < tileSize; m++) {
				Vector2 v = cct.vAve [n, m];
				float r = cct.rho [n, m];

				if (r != 0) {
					v /= r;
				}
				writeDataToPoint_vAve (n, m, v);
			}
		}
	}

	private void computeSpeedField (CC_Tile cct)
	{
		for (int n = 0; n < tileSize; n++) {
			for (int m = 0; m < tileSize; m++) {
				for (int d = 0; d < DIR_ENWS.Length; d++) {
					cct.f [n, m] [d] = computeSpeedFieldPoint (n, m, cct, DIR_ENWS [d]);
				}
			}
		} 
	}

	// IMPORTANT: in this function call, x and y are LOCAL to the tile
	private float computeSpeedFieldPoint (int tileX, int tileY, CC_Tile cct, Vector2 direction)
	{
		int xLocalInto = tileX + (int)direction.x;
		int yLocalInto = tileY + (int)direction.y;

		int xGlobalInto = cct.myLoc.x + xLocalInto;
		int yGlobalInto = cct.myLoc.y + yLocalInto;

		// if we're looking off the map, dont store this value
		if (!isPointValid (xGlobalInto, yGlobalInto)) {
			return CCvals.f_speedMin;
		}

		// otherwise, run the speed field calculation
		float ff = 0, ft = 0, fv = 0;
		float r;
		// test to see if the point we're looking INTO is in another tile, and if so, pull it
		if ((xLocalInto < 0) || (xLocalInto > tileSize - 1) || (yLocalInto < 0) || (yLocalInto > tileSize - 1)) {
			r = readDataFromPoint_g (xGlobalInto, yGlobalInto);
		} else {
			r = cct.rho [xLocalInto, yLocalInto];	
		}

		// test the density INTO WHICH we move: 
		if (r < CCvals.f_rhoMin) {				// rho < rho_min calc
			ft = computeTopographicalSpeed (tileX, tileY, cct.dh, direction);
			ff = ft;
		} else if (r > CCvals.f_rhoMax) {		// rho > rho_max calc
			fv = computeFlowSpeed (xGlobalInto, yGlobalInto, direction);
			ff = fv;
		} else {						// rho in-between calc
			fv = computeFlowSpeed (xGlobalInto, yGlobalInto, direction);
			ft = computeTopographicalSpeed (tileX, tileY, cct.dh, direction);
			ff = ft + (r - CCvals.f_rhoMin) / (CCvals.f_rhoMax - CCvals.f_rhoMin) * (fv - ft);
		}

		return Mathf.Max (CCvals.f_speedMin, ff);
	}


	private float computeTopographicalSpeed (int x, int y, Vector2[,] dh, Vector2 direction)
	{
		// first, calculate the gradient in the direction we are looking. By taking the dot with Direction,
		// we extract the direction we're looking and assign it a proper sign
		// i.e. if we look left (x=-1) we want -dhdx(x,y), because the gradient is assigned with a positive x
		// 		therefore:		also, Vector.left = [-1,0]
		//						Vector2.Dot(Vector.left, dh[x,y]) = -dhdx;
		float dhInDirection = Vector2.Dot (direction, dh [x, y]);
		// calculate the speed field from the equation
		return (CCvals.f_speedMax + (dhInDirection - CCvals.f_slopeMin) / (CCvals.f_slopeMax - CCvals.f_slopeMin) * (CCvals.f_speedMin - CCvals.f_speedMax));
	}

	private float computeFlowSpeed (int xI, int yI, Vector2 direction)
	{
		// the flow speed is simply the average velocity field of the region INTO WHICH we are looking,
		// dotted with the direction vector
		return Mathf.Max (CCvals.f_speedMin, Vector2.Dot (readDataFromPoint_vAve (xI,yI), direction));
	}

	private void computeCostField (CC_Tile cct)
	{
		for (int n = 0; n < tileSize; n++) {
			for (int m = 0; m < tileSize; m++) {
				for (int d = 0; d < DIR_ENWS.Length; d++) {
					cct.C [n, m] [d] = computeCostFieldValue (n, m, d, DIR_ENWS [d], cct);
				}
			}
		} 
	}

	private float computeCostFieldValue (int tileX, int tileY, int d, Vector2 direction, CC_Tile cct)
	{
		int xLocalInto = tileX + (int)direction.x;
		int yLocalInto = tileY + (int)direction.y;

		int xGlobalInto = cct.myLoc.x + xLocalInto;
		int yGlobalInto = cct.myLoc.y + yLocalInto;

		// if we're looking in an invalid direction, dont store this value
		if (!isPointValid (xGlobalInto, yGlobalInto) || (cct.f [tileX, tileY] [d] == 0)) {
			return Mathf.Infinity;
		}

		// test to see if the point we're looking INTO is in another tile, and if so, pull it
		float gP;
		if ((xLocalInto < 0) || (xLocalInto > tileSize - 1) || (yLocalInto < 0) || (yLocalInto > tileSize - 1)) {
			gP = readDataFromPoint_gP (xGlobalInto, yGlobalInto);
		} else {
			gP = cct.gP [xLocalInto, yLocalInto];	
		}

		float cost = (cct.f [tileX, tileY] [d] * CCvals.C_alpha + CCvals.C_beta + gP * CCvals.C_gamma) / cct.f [tileX, tileY] [d];

		return cost;
	}


	// ******************************************************************************
	//			TOOLS AND UTILITIES
	//******************************************************************************
	private float[,]  linear1stOrderSplat (Vector2 v, float scalar)
	{
		return linear1stOrderSplat (v.x, v.y, scalar);
	}

	private float[,] linear1stOrderSplat (float x, float y, float scalar)
	{
		float[,] mat = new float[2, 2];

		int xInd = (int)Mathf.Floor (x);
		int yInd = (int)Mathf.Floor (y);

		float delx = x - xInd;
		float dely = y - yInd;

		// use += to stack density field up
		if (isPointValid (xInd, yInd)) {
			mat [0, 0] += Mathf.Min (1 - delx, 1 - dely) * scalar;
		}
		if (isPointValid (xInd + 1, yInd)) {
			mat [1, 0] += Mathf.Min (delx, 1 - dely) * scalar;
		}
		if (isPointValid (xInd, yInd + 1)) {
			mat [0, 1] += Mathf.Min (1 - delx, dely) * scalar;
		}
		if (isPointValid (xInd + 1, yInd + 1)) {
			mat [1, 1] += Mathf.Min (delx, dely) * scalar;
		}

		return mat;
	}

	bool isPointValid (fastLocation l)
	{
		return isPointValid (l.x, l.y);
	}

	bool isPointValid (Vector2 v)
	{
		return isPointValid ((int)v.x, (int)v.y);
	}

	bool isPointValid (int x, int y)
	{
		// check to make sure the point is not outside the grid
		if ((x < 0) || (y < 0) || (x > _mapX - 1) || (y > _mapY - 1)) {
			return false;
		}
		// check to make sure the point is not on a place of absolute discomfort (like inside a building)
		// check to make sure the point is not in a place dis-allowed by terrain (slope)
		if (readDataFromPoint_g (x, y) == 1) {
			return false;
		}

		return true;
	}

	// ******************************************************************************************
	// 				functions used for reading and writing to tiles
	// ******************************************************************************************
	private CC_Tile getLocalTile (Location l)
	{	// define a default return value in case the location isnt found
		Location temp = new Location(0,0);
		foreach(Location L in _tiles.Keys) {
			if (L == l) {
				temp = L;
			}
		}
		return _tiles [temp];
	}	

	private void writeDataToPoint_g (int xGlobal, int yGlobal, float val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		localTile.writeData_g (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_dh (int xGlobal, int yGlobal, Vector2 val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		localTile.writeData_dh (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_rho (int xGlobal, int yGlobal, float val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		localTile.writeData_rho (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_gP (int xGlobal, int yGlobal, float val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		localTile.writeData_gP (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_vAve (int xGlobal, int yGlobal, Vector2 val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		_tiles [localTile.myLoc].writeData_vAve (xTile, yTile, val);
//		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_f (int xGlobal, int yGlobal, Vector4 val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		localTile.writeData_f (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_C (int xGlobal, int yGlobal, Vector4 val)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		localTile.writeData_C (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}


	private float readDataFromPoint_g (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		float f = localTile.readData_g (xTile, yTile);
		return f;
	}
	private Vector2 readDataFromPoint_dh (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		Vector2 f = localTile.readData_dh (xTile, yTile);
		return f;
	}
	private float readDataFromPoint_rho (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		float f = localTile.readData_rho (xTile, yTile);
		return f;
	}
	private float readDataFromPoint_gP (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		float f = localTile.readData_gP (xTile, yTile);
		return f;
	}
	private Vector2 readDataFromPoint_vAve (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		Vector2 f = localTile.readData_vAve (xTile, yTile);
		return f;
	}
	private Vector4 readDataFromPoint_f (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		Vector4 f = localTile.readData_f (xTile, yTile);
		return f;
	}
	private Vector4 readDataFromPoint_C (int xGlobal, int yGlobal)
	{
		Location l = new Location (Mathf.FloorToInt (((float)xGlobal) / ((float)tileSize)), 
			Mathf.FloorToInt (((float)yGlobal) / ((float)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x;
		int yTile = yGlobal - l.y;
		Vector4 f = localTile.readData_C (xTile, yTile);
		return f;
	}
}
