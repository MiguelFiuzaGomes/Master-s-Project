using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BiomeMapGenerator
{
   private BiomeEvaluator evaluator;
   private List<SO_Biome> biomes;

   public BiomeMapGenerator(List<SO_Biome> biomes)
   {
      this.biomes = biomes;
      evaluator = new BiomeEvaluator(biomes);
   }

   public SO_Biome[,] GenerateBiomeMap(float[,] heightMap, float[,] temperatureMap, float[,] humidityMap)
   {
      SO_Biome deepOcean = biomes.First(b=> b.name == "Deep Ocean");
      SO_Biome shallows = biomes.First(b=> b.name == "Shallows");

      float seaLevel = shallows.maximumHeight;
      float deepSeaLevel = deepOcean.maximumHeight;
      
      int width = heightMap.GetLength(0);
      int height = heightMap.GetLength(1);
      SO_Biome[,] biomeMap = new SO_Biome[width, height];

      for (int y = 0; y < height; y++)
      {
         for (int x = 0; x < width; x++)
         {
            // "Flood fill" everything bellow sea level
            if(heightMap[x, y] < seaLevel)
               biomeMap[x, y] = shallows;
            
            if(heightMap[x, y] < deepSeaLevel)
               biomeMap[x, y] = deepOcean;
            else
            {
               biomeMap[x, y] = evaluator.EvaluateBestMatch(
                  heightMap[x, y],
                  temperatureMap[x, y],
                  humidityMap[x, y]);
            }
         }
      }
      

      return biomeMap;
   }
   
}
