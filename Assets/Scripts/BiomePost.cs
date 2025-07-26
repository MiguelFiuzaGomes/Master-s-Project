using System.Collections;
using System.Collections.Generic;
using System.Xml;
using TreeEditor;
using Unity.Mathematics;
using UnityEngine;

public static class BiomePost
{
    public static SO_Biome[,] PruneTinyRegions(SO_Biome[,] map, int minSameNeighbours = 3)
    {
        int width = map.GetLength(0), height = map.GetLength(1);
        
        
        SO_Biome[,] output = map.Clone() as SO_Biome[,];
        
        bool[,] visited = new bool[map.GetLength(0), map.GetLength(1)];
        
        // Neighbourhood 4x4
        int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
        
        
        // temporary storage
        Stack<(int, int)> stack = new Stack<(int x, int y)>();
        List<(int, int)> regionTiles = new List<(int x, int y)>();
        Dictionary<SO_Biome, int> neighbours = new Dictionary<SO_Biome, int>();


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if(visited[x, y]) continue;
                
                SO_Biome biome = map[x, y];
                regionTiles.Clear();
                stack.Push((x, y));
                visited[x, y] = true;


                while (stack.Count > 0)
                {
                    var (cx, cy) = stack.Pop();
                    regionTiles.Add((cx, cy));
                    
                    // explore 4 neighbours
                    for (int i = 0; i < dx.Length; i++)
                    {
                        int nx = cx + dx[i], ny = cy + dy[i];

                        // Exclude out of bounds
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        
                        // Exclude visited
                        if(visited[nx, ny]) continue;
                        
                        visited[nx, ny] = true;
                        stack.Push((nx, ny));
                    }
                }

                // Skip large regions
                if (regionTiles.Count >= minSameNeighbours)
                    continue;
                
                // Pick the most common neightbour
                neighbours.Clear();

                foreach (var (cx, cy) in regionTiles)
                {
                    for (int i = 0; i < dx.Length; i++)
                    {
                        int nx = cx + dx[i], ny = cy + dy[i];
                        
                        // Exclude out of bounds 
                        if(nx < 0 || nx >= width || ny < 0 || ny >= height) continue;
                        
                        SO_Biome neighbourBiome = map[nx, ny];
                        
                        // Skip internal bounds
                        if(neighbourBiome == biome) continue;

                        neighbours[neighbourBiome] = neighbours.TryGetValue(neighbourBiome, out var c) ? c + 1 : 1;
                    }
                }
                
                // find the neighbour with max count
                SO_Biome fallback = biome;
                int best = -1;

                foreach (KeyValuePair<SO_Biome, int> kvp in neighbours)
                {
                    if (kvp.Value > best)
                    {
                        best = kvp.Value;
                        fallback = kvp.Key;
                    }
                }
                
                // reassign all tiles in this region
                foreach(var (tx, ty) in regionTiles)
                    output[tx, ty] = fallback;
            }
        }
        
        return output;
    }

    public static float[,] ReapplyHeightByBiome(float[,] heightMap, SO_Biome[,] biomeMap)
    {
        // Get width and height
        int width = heightMap.GetLength(0), height = heightMap.GetLength(1);
        
        // output of corrected heightMap
        float[,] corrected = new float[width, height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // store current biome and height for ease of access
                SO_Biome currentBiome = biomeMap[x, y];
                float currentHeight = heightMap[x, y];

                // fallback: leave untouched
                if (currentBiome == null)
                {
                    corrected[x, y] = currentHeight;
                }
                else
                {
                    // clamp the value to the biome's height band
                    float minH = currentBiome.minimumHeight;
                    float maxH = currentBiome.maximumHeight;
                    float clamped = Mathf.Clamp(currentHeight, minH, maxH);
                    
                    corrected[x, y] = clamped;
                }
            }
        }

        return corrected;
    }
}
