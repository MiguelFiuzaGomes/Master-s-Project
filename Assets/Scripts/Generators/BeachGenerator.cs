using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class BeachGenerator 
{
    public static SO_Biome[,] CarveBeach(SO_Biome[,] biomeMap, float[,] heightMap, SO_Biome shallows, SO_Biome beach, SO_Biome deepOcean)
    {
        int width = biomeMap.GetLength(0);
        int height = biomeMap.GetLength(1);
        
        SO_Biome[,] output = biomeMap.Clone() as SO_Biome[,];
        
        // Find all Shallows cells that touch deep ocean
        bool[,] shoreline = new bool[width, height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if(biomeMap[x, y] != shallows) continue;

               TryCarve(biomeMap, width, height, x-1, y, shallows, deepOcean, beach, ref output);
               TryCarve(biomeMap, width, height, x+1, y, shallows, deepOcean, beach, ref output);
               TryCarve(biomeMap, width, height, x, y-1, shallows, deepOcean, beach, ref output);
               TryCarve(biomeMap, width, height, x, y+1, shallows, deepOcean, beach, ref output);
            }
        }
        
        return output;
    }
    
    // Helper function
    // returns true if this biome is not a water biome
    private static bool IsLand(SO_Biome biome, SO_Biome deepOcean, SO_Biome shallows)
    => biome != deepOcean && biome != shallows;

    private static void TryCarve(SO_Biome[,] biomeMap, int width, int height, int cx, int cy, SO_Biome shallows, SO_Biome deepOcean, SO_Biome beach, ref SO_Biome[,] output)
    {
        if(cx < 0 || cx >= width || cy < 0 || cy >= height) return;
        SO_Biome neighbour = biomeMap[cx, cy];
        
        // only turn true "land" into beach
        if(neighbour!= shallows && neighbour != deepOcean)
            output[cx, cy] = beach;
            
    }
}
