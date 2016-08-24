using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;


public class CCDynamicGlobalFields
{
	public int tileSize = 20;

	// the meat of the CC Dynamic Global Fields computer
	private Dictionary<Location, CC_Tile> _tiles;
	private List<CC_Unit> _units;

	public Map_Data_Package theMapData;

	// map dimensions
	private int _mapX, _mapY;

	// cache the current tile to reduce dictionary calls
	private CC_Tile current_Tile;
	private Location current_Tile_Loc;

	// cached 2x2 float[] for linear1stOrderSplat (GC redux)
	private float[,] mat = new float[2,2];

	// this array of Vect2's correlates to our data format: Vector4(x, y, z, w) = (+x, +y, -x, -y)
	Vector2[] DIR_ENWS = new Vector2[] { Vector2.right, Vector2.up, Vector2.left, Vector2.down };

	// ******************************************************************************************
	// 				PUBLIC ACCESSORS and CONSTRUCTORS
	// ******************************************************************************************
	public CCDynamicGlobalFields ()
	{
		_tiles = new Dictionary<Location, CC_Tile>(); // (new LocationComparator());
		_units = new List<CC_Unit> ();

		theMapData = new Map_Data_Package ();
	}

	public void updateCCUnits() {
		for (int i = 0; i < _units.Count; i++) {
			_units [i].updatePhysics ();
		}
	}

	public void setTileSize (int s) {
		tileSize = s;
	}

	public void setMapData (Map_Data_Package data) {
		theMapData = data;
		_mapX = data.getMapX();
		_mapY = data.getMapY();
	}

	public bool initiateTiles () {
		// take map dimensions
		// if tileSize and map dimensions dont fit perfectly, drop a flag
		// otherwise, create all the tiles

		// make sure the map dimensions are divisible by tileSize
		if ((((float)_mapX) % ((float)tileSize) != 0) || 
			(((float)_mapY) % ((float)tileSize) != 0)) {
			// this should NEVER HAPPEN, so send an error if it does
			return false;
		} else {
			Location loc;

			int numTilesX = _mapX / tileSize;
			int numTilesY = _mapY / tileSize;

			// instantiate all our tiles
			for (int x = 0; x < numTilesX; x++) {
				for (int y = 0; y < numTilesY; y++) {
					// create a new tile based on this location
					loc = new Location (x, y);
					CC_Tile cct = new CC_Tile (tileSize, loc);
					_tiles.Add (loc, cct);
				}
			}

			if (_tiles.Keys.Count > 0) {
				current_Tile = _tiles[new Location(0,0)];
				current_Tile_Loc = current_Tile.myLoc;
			}
		}

		return true;
	}

	public void updateTiles ()
	{	
		// first, clear the tiles
		foreach (CC_Tile cct in _tiles.Values) {	
			if (cct.UPDATE_TILE) {
				cct.resetTile ();
			}
		}
		// update the unit specific elements (rho, vAve, g_P)
		foreach (CC_Unit ccu in _units) {
			// (1) density field and velocity
			computeDensityField (ccu); 
			// predictive discomfort is only applied to moving units
			if (ccu.getVelocity () != Vector2.zero) {	
				// (2) predictive discomfort field
				applyPredictiveDiscomfort (CCvals.gP_predictiveSeconds, ccu);	
			}
		}
		// these next values are derived from rho, vAve, and g_P, so we simply iterate
		// through the tiles and ONLY update the ones that have had their values changed
		foreach (CC_Tile cct in _tiles.Values) {
			if (cct.UPDATE_TILE) {			
				// (3) 	now that the velocity field and density fields are implemented,
				// 		divide the velocity by density to get average velocity field
				computeAverageVelocityField (cct);
				// (4)	now that the average velocity field is computed, and the density
				// 		field is in place, we calculate the speed field, f
				computeSpeedField (cct);
				// (5) 	the cost field depends only on f and g, so it can be computed in its
				//		entirety now as well
				computeCostField (cct);
			}
		}
	}

	// 0000000000000000000000000000000000000000000000000000000000
	// 0000000000000000000000000000000000000000000000000000000000
	public void drawRhoOnTile(Location l) {
		CC_Tile cct = getLocalTile (l);
		NavSystem.S._DEBUG_VISUAL_plotTileFields (new Vector2 (cct.myLoc.x, cct.myLoc.y), cct.gP);	
//		NavSystem.S.theMapAnalyzer.printOutMatrix (cct.f);
	}
	// 0000000000000000000000000000000000000000000000000000000000
	// 0000000000000000000000000000000000000000000000000000000000

	public void addNewCCUnit (CC_Unit ccu)
	{
		_units.Add (ccu);
	}

	public void removeCCUnit (CC_Unit ccu)
	{
		_units.Remove (ccu);
	}

	public CC_Map_Package buildCCMapPackage (Rect r) {
		float[,] gt;
		Vector4[,] ft, Ct;

		int xs = (int)Math.Floor((double)r.x);
		int ys = (int)Math.Floor((double)r.y);

		int xf = (int)Math.Ceiling((double)(r.x + r.width));
		int yf = (int)Math.Ceiling((double)(r.y + r.height));

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
				gt [xI, yI] = theMapData.getDiscomfortMap (xs + xI, ys + yI);
				ft [xI, yI] = readDataFromPoint_f (xs + xI, ys + yI);
				Ct [xI, yI] = readDataFromPoint_C (xs + xI, ys + yI);
			}
		}
		CC_Map_Package map = new CC_Map_Package (gt, ft, Ct);
		return map;
	}
	// ******************************************************************************************
	// ******************************************************************************************
	// ******************************************************************************************
	// 							FIELD SOLVING FUNCTIONS
	// ******************************************************************************************
	private void computeDensityField (CC_Unit cc_u)
	{
		Vector2[] cc_u_pos = cc_u.getPositions ();

		for (int i = 0; i < cc_u_pos.Length; i++) {
			int xInd = (int)Math.Floor ((double)cc_u_pos[i].x);
			int yInd = (int)Math.Floor ((double)cc_u_pos[i].y);

			float[,] rho = linear1stOrderSplat (cc_u_pos[i].x, cc_u_pos[i].y, CCvals.rho_sc);

			if (isPointValid (xInd, yInd)) {
				float rt = readDataFromPoint_rho(xInd, yInd);
				writeDataToPoint_rho (xInd, yInd, rt + rho [0, 0]);
			}
			if (isPointValid (xInd + 1, yInd)) {
				float rt = readDataFromPoint_rho(xInd + 1, yInd);
				writeDataToPoint_rho (xInd + 1, yInd, rt + rho [1, 0]);
			}
			if (isPointValid (xInd, yInd + 1)) {
				float rt = readDataFromPoint_rho(xInd, yInd + 1);
				writeDataToPoint_rho (xInd, yInd + 1, rt + rho [0, 1]);
			}
			if (isPointValid (xInd + 1, yInd + 1)) {
				float rt = readDataFromPoint_rho(xInd + 1, yInd + 1);
				writeDataToPoint_rho (xInd + 1, yInd + 1, rt + rho [1, 1]);
			}

			computeVelocityFieldPoint (xInd, yInd, cc_u.getVelocity ());
			computeVelocityFieldPoint (xInd + 1, yInd, cc_u.getVelocity ());
			computeVelocityFieldPoint (xInd, yInd + 1, cc_u.getVelocity ());
			computeVelocityFieldPoint (xInd + 1, yInd + 1, cc_u.getVelocity ());
		}
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

		Vector2[] cc_u_pos = cc_u.getPositions ();

		for (int k = 0; k < cc_u_pos.Length; k++) {

			Vector2 xprime = cc_u_pos[k]  + cc_u.getVelocity () * numSec;

			float vfMag = Vector2.Distance (cc_u_pos[k], xprime);

			for (int i = 5; i < vfMag; i++) {
				newLoc = Vector2.MoveTowards (cc_u_pos[k], xprime, i);

				sc = (vfMag - i) / vfMag;				// inverse scale
				float[,] gP = linear1stOrderSplat (newLoc, sc * CCvals.gP_weight);

				int xInd = (int)Math.Floor((double)newLoc.x);
				int yInd = (int)Math.Floor((double)newLoc.y);

				if (isPointValid (xInd, yInd)) {
					float gPt = readDataFromPoint_gP (xInd, yInd);
					writeDataToPoint_gP (xInd, yInd, gPt + gP [0, 0]);
				}
				if (isPointValid (xInd + 1, yInd)) {
					float gPt = readDataFromPoint_gP (xInd + 1, yInd);
					writeDataToPoint_gP (xInd + 1, yInd, gPt + gP [1, 0]);
				}
				if (isPointValid (xInd, yInd + 1)) {
					float gPt = readDataFromPoint_gP (xInd, yInd + 1);
					writeDataToPoint_gP (xInd, yInd + 1, gPt +  gP [0, 1]);
				}
				if (isPointValid (xInd + 1, yInd + 1)) {
					float gPt = readDataFromPoint_gP (xInd + 1, yInd + 1);
					writeDataToPoint_gP (xInd + 1, yInd + 1, gPt + gP [1, 1]);
				}
			}
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
				cct.vAve [n, m] = v;
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

		int xGlobalInto = cct.myLoc.x*tileSize + xLocalInto;
		int yGlobalInto = cct.myLoc.y*tileSize + yLocalInto;

		// otherwise, run the speed field calculation
		float ff = 0, ft = 0, fv = 0;
		float r;
		// test to see if the point we're looking INTO is in another tile, and if so, pull it
		if ((xLocalInto < 0) || (xLocalInto > tileSize - 1) || (yLocalInto < 0) || (yLocalInto > tileSize - 1)) {
			// if we're looking off the map, dont store this value
			if (!isPointValid (xGlobalInto, yGlobalInto)) {
				return CCvals.f_speedMin;
			}
			r = readDataFromPoint_rho (xGlobalInto, yGlobalInto);
		} else {
			r = cct.rho [xLocalInto, yLocalInto];	
		}

		// test the density INTO WHICH we move: 
		if (r < CCvals.f_rhoMin) {				// rho < rho_min calc
			ft = computeTopographicalSpeed (tileX, tileY, theMapData.getHeightGradientMap(tileX, tileY), direction);
			ff = ft;
		} else if (r > CCvals.f_rhoMax) {		// rho > rho_max calc
			fv = computeFlowSpeed (xGlobalInto, yGlobalInto, direction);
			ff = fv;
		} else {						// rho in-between calc
			fv = computeFlowSpeed (xGlobalInto, yGlobalInto, direction);
			ft = computeTopographicalSpeed (tileX, tileY, theMapData.getHeightGradientMap(tileX, tileY), direction);
			ff = ft + (r - CCvals.f_rhoMin) / (CCvals.f_rhoMax - CCvals.f_rhoMin) * (fv - ft);
		}

		return Math.Max (CCvals.f_speedMin, ff);
	}


	private float computeTopographicalSpeed (int x, int y, Vector2 dh, Vector2 direction)
	{
		// first, calculate the gradient in the direction we are looking. By taking the dot with Direction,
		// we extract the direction we're looking and assign it a proper sign
		// i.e. if we look left (x=-1) we want -dhdx(x,y), because the gradient is assigned with a positive x
		// 		therefore:		also, Vector.left = [-1,0]
		//						Vector2.Dot(Vector.left, dh[x,y]) = -dhdx;
		float dhInDirection = (direction.x * dh.x + direction.y * dh.y);
		// calculate the speed field from the equation
		return (CCvals.f_speedMax + (dhInDirection - CCvals.f_slopeMin) / (CCvals.f_slopeMax - CCvals.f_slopeMin) * (CCvals.f_speedMin - CCvals.f_speedMax));
	}

	private float computeFlowSpeed (int xI, int yI, Vector2 direction)
	{
		// the flow speed is simply the average velocity field of the region INTO WHICH we are looking,
		// dotted with the direction vector
		Vector2 vAvePt = readDataFromPoint_vAve (xI,yI);
		float theDotPrd = (vAvePt.x * direction.x + vAvePt.y * direction.y);
		return Math.Max (CCvals.f_speedMin, theDotPrd);
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

		int xGlobalInto = cct.myLoc.x*tileSize + xLocalInto;
		int yGlobalInto = cct.myLoc.y*tileSize + yLocalInto;

		// if we're looking in an invalid direction, dont store this value
		if (cct.f [tileX, tileY] [d] == 0) {
			return Mathf.Infinity;
		} else if (!isPointValid (xGlobalInto, yGlobalInto)) {
			return Mathf.Infinity;
		}

		// test to see if the point we're looking INTO is in a DIFFERENT tile, and if so, pull it
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
	private float[,] linear1stOrderSplat (Vector2 v, float scalar)
	{
		return linear1stOrderSplat (v.x, v.y, scalar);
	}
	private float[,] linear1stOrderSplat (float x, float y, float scalar)
	{
		mat [0, 0] = 0;
		mat [0, 1] = 0;
		mat [1, 0] = 0;
		mat [1, 1] = 0;
		
		int xInd = (int)Math.Floor ((double)x);
		int yInd = (int)Math.Floor ((double)y);

		float delx = x - xInd;
		float dely = y - yInd;

		// use += to stack density field up
		if (isPointValid (xInd, yInd)) {
			mat [0, 0] += Math.Min (1 - delx, 1 - dely);
			mat [0, 0] *= scalar;
		}
		if (isPointValid (xInd + 1, yInd)) {
			mat [1, 0] += Math.Min (delx, 1 - dely);
			mat [1, 0] *= scalar;
		}
		if (isPointValid (xInd, yInd + 1)) {
			mat [0, 1] += Math.Min (1 - delx, dely);
			mat [0, 1] *= scalar;
		}
		if (isPointValid (xInd + 1, yInd + 1)) {
			mat [1, 1] += Math.Min (delx, dely) ;
			mat [1, 1] *= scalar;
		}

		return mat;
	}

	bool isPointValid (int x, int y)
	{
		// check to make sure the point is not outside the grid
		if ((x < 0) || (y < 0) || (x > _mapX - 1) || (y > _mapY - 1)) {
			return false;
		}
		// check to make sure the point is not on a place of absolute discomfort (like inside a building)
		// check to make sure the point is not in a place dis-allowed by terrain (slope)
		if (theMapData.getDiscomfortMap(x, y) == 1) {
			return false;
		}
		return true;
	}

	// ******************************************************************************************
	// 				functions used for reading and writing to tiles
	// ******************************************************************************************
	private CC_Tile getLocalTile (Location l)
	{	
		if (current_Tile_Loc == l) {
			return current_Tile;
		}

		if (_tiles.ContainsKey (l)) {
			current_Tile = _tiles [l];
			current_Tile_Loc = current_Tile.myLoc;
			return current_Tile;
		} else {
			return (new CC_Tile (0,l));
		}
	}	

	// *** read ops ***
	private float readDataFromPoint_rho (int xGlobal, int yGlobal)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		float f = localTile.readData_rho (xTile, yTile);
		return f;
	}
	private float readDataFromPoint_gP (int xGlobal, int yGlobal)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		float f = localTile.readData_gP (xTile, yTile);
		return f;
	}
	private Vector2 readDataFromPoint_vAve (int xGlobal, int yGlobal)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		Vector2 f = localTile.readData_vAve (xTile, yTile);
		return f;
	}
	private Vector4 readDataFromPoint_f (int xGlobal, int yGlobal)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		Vector4 f = localTile.readData_f (xTile, yTile);
		return f;
	}
	private Vector4 readDataFromPoint_C (int xGlobal, int yGlobal)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		Vector4 f = localTile.readData_C (xTile, yTile);
		return f;
	}

	// *** write ops ***
	private void writeDataToPoint_rho (int xGlobal, int yGlobal, float val)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		localTile.writeData_rho (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_gP (int xGlobal, int yGlobal, float val)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		localTile.writeData_gP (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_vAve (int xGlobal, int yGlobal, Vector2 val)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		_tiles [localTile.myLoc].writeData_vAve (xTile, yTile, val);
	}
	private void writeDataToPoint_f (int xGlobal, int yGlobal, Vector4 val)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		localTile.writeData_f (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
	private void writeDataToPoint_C (int xGlobal, int yGlobal, Vector4 val)
	{
		Location l = new Location ( (int) Math.Floor (((double)xGlobal) / ((double)tileSize)), 
			(int) Math.Floor (((double)yGlobal) / ((double)tileSize)));
		CC_Tile localTile = getLocalTile (l);
		int xTile = xGlobal - l.x * tileSize;
		int yTile = yGlobal - l.y * tileSize;
		localTile.writeData_C (xTile, yTile, val);
		_tiles [localTile.myLoc] = localTile;
	}
}
