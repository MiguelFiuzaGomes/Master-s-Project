using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public static class TextureGenerator 
{
    public static Texture2D TextureFromColorMap(Color[] colorMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        
        texture.SetPixels(colorMap);
        texture.Apply();
        
        return texture;
    }
    
    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, heightMap[x, y]); 
            }
        }

        return TextureFromColorMap(colourMap, width, height);
    }

    public static Texture2D VoronoiTexture(int gridSize, Color[] zoneColors, int width, int height)
    {
        //Set up
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;
        int pixelPerCell = width / gridSize;
        
        // Generate Points
        Vector2Int[,] pointPositions = new Vector2Int[width, height];
        GenerateVoronoiSites(pointPositions, pixelPerCell, gridSize);
        
        
        // Return full Voronoi
        return texture;
    }

    static void GenerateVoronoiSites(Vector2Int[,] pointPositions, int ppC, int gridSize)
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                pointPositions[i, j] = new Vector2Int(i * ppC + Random.Range(0, ppC), j * ppC + Random.Range(0, ppC));
            }
        }
    }

    public static Texture2D TextureFromTemperature(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.blue, Color.red, heightMap[x, y]); 
            }
        }

        return TextureFromColorMap(colourMap, width, height);
    }
    
    public static Texture2D TextureFromHumidity(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);
        
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[y * width + x] = Color.Lerp(Color.cyan, Color.blue, heightMap[x, y]); 
            }
        }

        return TextureFromColorMap(colourMap, width, height);
    }
}
