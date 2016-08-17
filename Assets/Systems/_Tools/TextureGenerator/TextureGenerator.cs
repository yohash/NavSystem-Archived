using UnityEngine;
using System.Collections;

public static class TextureGenerator{

	public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height){
		Texture2D texture = new Texture2D (width, height);
		texture.filterMode = FilterMode.Point;
		texture.wrapMode = TextureWrapMode.Clamp;
		texture.SetPixels (colorMap);
		texture.Apply ();
		return texture;
	}

	public static Texture2D TextureFromMap(float[,] heightMap){
		int width = heightMap.GetLength (0);
		int height = heightMap.GetLength (1);

		//Create a map with all the pixels colors predefined (faster than applying each pixel one-by-one)
		Color[] colorMap = new Color[width * height];
		for (int y = 0; y < height; y++) {
			for (int x = 0; x < width; x++) {
				//Assign the pixel a color based on its value
				colorMap [y * width + x] = Color.Lerp (Color.black, Color.white, heightMap [x, y]);
			}
		}
		return TextureFromColorMap (colorMap, width, height);
	}

}
