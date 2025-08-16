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
        
        // Work with a copy so we don't change the main as we go
        SO_Biome[,] output = map.Clone() as SO_Biome[,];
        
        bool[,] visited = new bool[width, height];
        
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
                        
                        if (visited[nx, ny]) continue;
                        if (!map[nx, ny].Equals(biome)) continue;
                        
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
                
                // If no different neighbours, continue
                if(neighbours.Count == 0) continue;
                
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

    
}
