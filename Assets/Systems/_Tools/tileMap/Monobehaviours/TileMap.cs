using UnityEngine;
using System.Collections;

public class TileMap : MonoBehaviour {

	public MeshFilter mesh_filter;
	public MeshRenderer mesh_renderer;
	public MeshCollider mesh_collider;

	public int mapX;
	public int mapZ;

	public float tileSize = 1f;

	void Awake () {
		mesh_filter = GetComponent<MeshFilter> ();
		mesh_renderer = GetComponent<MeshRenderer> ();
		mesh_collider = GetComponent<MeshCollider> ();
	}

	public void BuildTexture(Texture2D tex) {
		mesh_renderer.sharedMaterials [0].mainTexture = tex;
	}

	public void BuildMesh (Vector2 corner, float[,] mat) {

		mapX = mat.GetLength (0)-1;
		mapZ = mat.GetLength (1)-1;

		int numTiles = mapX * mapZ;
		int numTris = numTiles * 2;

		int vSize_x = mapX + 1;
		int vSize_z = mapZ + 1;
		int numVerts = vSize_x * vSize_z;

		// generate mesh data
		Vector3[] vertices = new Vector3[numVerts];
		Vector3[] normals = new Vector3[numVerts];
		Vector2[] uv = new Vector2[numVerts];

		int[] triangles = new int[numTris * 3];

		int x, z;
		for (z = 0; z < vSize_z; z++) {
			for (x = 0; x < vSize_x; x++) {
				float h;
				if (z > mapZ - 1 && x > mapX - 1) {
					h = mat [x - 1, z - 1];
				} else if (z > mapZ - 1) {
					h = mat [x, z - 1];
				} else if (x > mapX - 1) {
					h = mat [x - 1, z];
				} else {					
					h = mat [x, z];
				}
				vertices [z * vSize_x + x] = new Vector3 (corner.x + x * tileSize, h+.1f, corner.y + z * tileSize);
				normals [z * vSize_x + x] = Vector3.up;
				uv [z * vSize_x + x] = new Vector2 ((float)x / vSize_x, (float)z / vSize_z);
			}
		}

		for (z = 0; z < mapZ; z++) {
			for (x = 0; x < mapX; x++) {
				int squareIndex = z * mapX + x;
				int triOffset = squareIndex * 6;

				triangles [triOffset + 0] = z * vSize_x + x + 0;
				triangles [triOffset + 1] = z * vSize_x + x + vSize_x + 0;
				triangles [triOffset + 2] = z * vSize_x + x + vSize_x + 1;

				triangles [triOffset + 3] = z * vSize_x + x + 0;
				triangles [triOffset + 4] = z * vSize_x + x + vSize_x + 1;
				triangles [triOffset + 5] = z * vSize_x + x + 1;
			}
		}

		// create a new mesh and populate with data
		Mesh mesh = new Mesh ();
		mesh.vertices = vertices;
		mesh.triangles = triangles;
		mesh.normals = normals;
		mesh.uv = uv;

		// assign our mesh to our filter/renderer
		mesh_filter.mesh = mesh;
	}
}
