using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BiomeMapGenerator
{
   private BiomeEvaluator evaluator;

   public BiomeMapGenerator(List<SO_Biome> biomes)
   {
      evaluator = new BiomeEvaluator(biomes);
   }

   public SO_Biome[,] GenerateBiomeMap(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap)
   {
      int width = heightMap.GetLength(0);
      int height = heightMap.GetLength(1);
      SO_Biome[,] biomeMap = new SO_Biome[width, height];

      for (int y = 0; y < height; y++)
      {
         for (int x = 0; x < width; x++)
         {
            biomeMap[x, y] = evaluator.EvaluateBestMatch(
               heightMap[x, y],
               temperatureMap[x, y],
               humidityMap[x, y]);
         }
      }

      return biomeMap;
   }
   
}
