using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class meshLineGenerator : MonoBehaviour {

	public float lineWidth = 0.1f;
	public float groundYOffset = 0.1f;

	public bool ____forDebugPurpose___;
	public Vector3[] linePoints;
	public Vector3[] lineNormals;
	int numPoints, numSegments;

	public Vector3[] newVerts;
	public Vector2[] newUV;
	public int[] newTriangles;

	MeshFilter meshFilter;
	Mesh mesh;
	Transform tr;

	Material mat;
	Renderer rend;

	void Awake () {
		meshFilter = GetComponent<MeshFilter>();
		mesh = meshFilter.mesh;
		tr = GetComponent<Transform>();
		rend = GetComponent<Renderer> ();

		mat = rend.material;
	}

	void Start () {
	}

	// ************************************************************************
	// 		PUBLIC ACCESSORS
	// ************************************************************************

	public void setLineWidth (float width) {
		lineWidth = width;
	}
	public void setGroundOffset (float offset) {
		groundYOffset = offset;
	}
	public void setColor(Color c) {
		mat.color = c;
		mat.SetColor ("_EmissionColor", c);
	}
	public void setLinePoints (Vector3[] points, Vector3[] normals, float offset) {
		setGroundOffset(offset);
		setLinePoints(points, normals);
	}
	public void setLinePoints (Vector3[] points, Vector3[] normals) {
		linePoints = points;
		lineNormals = normals;
		Vector3 yOff = new Vector3(0f, groundYOffset, 0f);
		for(int i=0; i<linePoints.Length; i++) {
			linePoints[i] += yOff;
		}
		numPoints = linePoints.Length;
		numSegments = numPoints-1;
	}

	public void generateMesh() {
		rebuildMesh();
	}

	// ************************************************************************
	// 		MESH BUILDING OPERATIONS
	// ************************************************************************
	void rebuildMesh() {

		newVerts = new Vector3[4 + (numSegments-1)*2];
		newUV = new Vector2[4 + (numSegments-1)*2];

		Vector3 xformL, localPointL = new Vector3(-lineWidth/2,0f,0f);
		Vector3 xformR, localPointR = new Vector3(lineWidth/2,0f,0f);

		Vector3 worldPoint1, worldPoint2, worldPoint3;

		Vector3 v1,v2,v3,v4;

		// initiate the first two triangles manually, so set proper initial path direction
		// and to more easily loop remaining new points in each respective triangle
		worldPoint1 = linePoints[0];
		worldPoint2 = linePoints[1];

		Vector3[] newPoints = steerNewLineSegments(worldPoint1,worldPoint1+(worldPoint2-worldPoint1),localPointL,localPointR);
		Vector3[] newOffset = alignNewLineSegmentsWithNormal(worldPoint1,worldPoint1+(worldPoint2-worldPoint1),lineNormals[0]);

		xformL = newPoints[0]+newOffset[0];
		xformR = newPoints[1]+newOffset[1];;

		v1 = worldPoint1 + xformL;
		v2 = worldPoint1 + xformR;

		newVerts[0] = v1-tr.position;
		newVerts[1] = v2-tr.position;

		if (linePoints.Length>2) {
			newPoints = steerNewLineSegments2ndOrder(worldPoint1,worldPoint2,linePoints[2],localPointL,localPointR);
			newOffset = alignNewLineSegmentsWithNormal(worldPoint1,worldPoint2,lineNormals[1]);
		} else {
			newPoints = steerNewLineSegments(worldPoint1,worldPoint2,localPointL,localPointR);
			newOffset = alignNewLineSegmentsWithNormal(worldPoint1,worldPoint2,lineNormals[1]);
		}

		xformL = newPoints[0]+newOffset[0];
		xformR = newPoints[1]+newOffset[1];

		v3 = worldPoint2 + xformL;
		v4 = worldPoint2 + xformR;

		newVerts[2] = v3-tr.position;
		newVerts[3] = v4-tr.position;

		newUV[0] = new Vector2(newVerts[0].x,newVerts[0].z);
		newUV[1] = new Vector2(newVerts[2].x,newVerts[2].z);

		for (int i=1; i<numSegments;i++) {
			v1 = v3;
			v2 = v4;

			worldPoint1 = worldPoint2;
			worldPoint2 = linePoints[i+1];

			if (i<(numSegments-1)) {
				worldPoint3 = linePoints[i+2];
			} else {
				worldPoint3 = worldPoint2 + (worldPoint2-worldPoint1);
			}
			// perform y-axis rotation to align local points with direction line is moving
			// perform x and z-axis rotations to align local points with surface normal
			newPoints = steerNewLineSegments2ndOrder(worldPoint1,worldPoint2,worldPoint3,localPointL,localPointR);
			newOffset = alignNewLineSegmentsWithNormal(worldPoint1,worldPoint2,lineNormals[i+1]);

			xformL = newPoints[0]+newOffset[0];
			xformR = newPoints[1]+newOffset[1];

			v3 = worldPoint2 + xformL;
			v4 = worldPoint2 + xformR;

			newVerts[2*i+2] = v3-tr.position;
			newVerts[2*i+3] = v4-tr.position;

			newUV[2*i] = new Vector2(newVerts[i].x,newVerts[i].z);
			newUV[2*i+1] = new Vector2(newVerts[i+2].x,newVerts[i+2].z);
		}

		newTriangles = new int[numSegments*6];
		for (int i=0; i<(numSegments);i++) {
			newTriangles[i*6] = i*2+1;
			newTriangles[i*6+1] = i*2;
			newTriangles[i*6+2] = i*2+2;

			newTriangles[i*6+3] = i*2+1;
			newTriangles[i*6+4] = i*2+2;
			newTriangles[i*6+5] = i*2+3;
		}

		mesh.Clear();
		mesh.vertices = newVerts;
		mesh.uv = newUV;
		mesh.triangles = newTriangles;
	}

	// ***********************************************************************************************
	// 			Mathematical HELPeR Functions
	// ***********************************************************************************************
	Vector3[] steerNewLineSegments2ndOrder(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 offsetL, Vector3 offsetR) {
		Vector3 dir1 = p2-p1;
		Vector3 dir2 = p3-p2;

		Vector3[] offsets = new Vector3[2];

		offsets[0] = offsetL;
		offsets[1] = offsetR;

		float roteAngle = Vector3.Angle (new Vector3(0f,0f,1f), dir1);
		if (dir1.x > 0) {
			roteAngle = 360f - roteAngle;
		}

		float insetAngle = Vector3.Angle (dir1, dir2);
		if (targetIsOnLEFT(dir1,dir2)) {insetAngle = -insetAngle;}

		roteAngle += insetAngle/2f;

		Vector3 temp;
		for (int i=0; i<offsets.Length; i++) {
			temp = offsets[i];
			temp.x = Mathf.Cos (roteAngle * Mathf.Deg2Rad) * offsets[i].x - Mathf.Sin (roteAngle * Mathf.Deg2Rad) * offsets[i].z;
			temp.z = Mathf.Sin (roteAngle * Mathf.Deg2Rad) * offsets[i].x + Mathf.Cos (roteAngle * Mathf.Deg2Rad) * offsets[i].z;
			offsets[i] = temp;
		}

		return offsets;
	}


	Vector3[] steerNewLineSegments(Vector3 p1, Vector3 p2, Vector3 offsetL, Vector3 offsetR) {
		Vector3 dir = p2-p1;
		Vector3[] offsets = new Vector3[2];

		offsets[0] = offsetL;
		offsets[1] = offsetR;

		float roteAngle = Vector3.Angle (new Vector3(0f,0f,1f), dir);
		if (dir.x < 0) {
			roteAngle = 360f - roteAngle;
		}
		Vector3 temp;
		for (int i=0; i<offsets.Length; i++) {
			temp = offsets[i];
			temp.x = Mathf.Cos (roteAngle * Mathf.Deg2Rad) * offsets[i].x - Mathf.Sin (roteAngle * Mathf.Deg2Rad) * offsets[i].z;
			temp.z = Mathf.Sin (roteAngle * Mathf.Deg2Rad) * offsets[i].x + Mathf.Cos (roteAngle * Mathf.Deg2Rad) * offsets[i].z;
			offsets[i] = temp;
		}

		return offsets;
	}

	Vector3[] alignNewLineSegmentsWithNormal(Vector3 p1, Vector3 p2, Vector3 n) {
		Vector3 dir = p2-p1;
		Vector3[] offsets = new Vector3[2];

		Vector3 rightSide = (Vector3.Cross(n, dir).normalized * lineWidth/2);

		float rightSideY = rightSide.y;
		float leftSideY = -rightSideY;

		offsets[0] = new Vector3(0f,leftSideY,0f);
		offsets[1] = new Vector3(0f,rightSideY,0f);

		return offsets;
	}


	// ****************************************************************************
	// FUNCTION targetIsOnLEFT - determines which side the vector v2 is on
	//				returns a bool to make this packageable in IF stmnts
	// 		inputs: v1, v2
	// 		output: TRUE if v1 is on the LEFT of unit v2
	public bool targetIsOnLEFT(Vector3 v1, Vector3 v2) {
		// find out if v1 is on left or right side of v2
		float d, x, x2, y, y2;
		x = v1.x;
		y = v1.z;
		x2 = v2.x;
		y2 = v2.z;

		d = (x) * (y2) - (y) * (x2);

		return (d < 0);
	}
}
