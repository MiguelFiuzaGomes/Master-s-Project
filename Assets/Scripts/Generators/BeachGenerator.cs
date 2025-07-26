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

                if ((x > 0 && biomeMap[x - 1, y]==deepOcean) ||
                    (x < width - 1 && biomeMap[x + 1, y]==deepOcean) ||
                    (y > 0 && biomeMap[x, y - 1]==deepOcean) ||
                    (y < height - 1 && biomeMap[x, y + 1]==deepOcean))
                {
                    shoreline[x, y] = true;
                }
            }
        }

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!shoreline[x,y]) continue;

                // 4-neighbor land check
                if (x > 0   && IsLand(biomeMap[x - 1, y], deepOcean, shallows))
                    output[x - 1, y] = beach;
                if (x < width -1 && IsLand(biomeMap[x + 1, y], deepOcean, shallows))
                    output[x + 1, y] = beach;
                if (y > 0   && IsLand(biomeMap[x, y - 1], deepOcean, shallows))
                    output[x, y - 1] = beach;
                if (y < height - 1 && IsLand(biomeMap[x, y + 1], deepOcean, shallows))
                    output[x, y + 1] = beach;
            }
        }

        return output;
    }
    
    // Helper function
    // returns true if this biome is not a water biome
    private static bool IsLand(SO_Biome biome, SO_Biome deepOcean, SO_Biome shallows)
    => biome != deepOcean && biome != shallows;
}
