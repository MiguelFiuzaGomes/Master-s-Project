using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class ColourMapGenerator : MonoBehaviour
{
    /*
     Summary:
        Converts a grid of SO_Biomes into a grid of color
        Flattens the 2D biomeMap into a 1D colour array
    */

    public static Color[] GenerateColourMap(SO_Biome[,] biomeMap)
    {
        int width = biomeMap.GetLength(0);
        int height = biomeMap.GetLength(1);
        
        Color[] colourMap = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                SO_Biome biome = biomeMap[x, y];

                colourMap[index] = biome != null ? biome.colour : Color.magenta;
            }
        }
        
        return colourMap;
    }
}
