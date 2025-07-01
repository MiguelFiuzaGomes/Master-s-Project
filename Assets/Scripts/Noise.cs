using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class Noise
{

   public enum NormalizeMode
   {
      Local, 
      Global, // estimating noise min and max
   }
   
   public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
   {
      float [,] noiseMap = new float[mapWidth, mapHeight];

      float maxPossibleHeight = 0;
      float amplitude = 1;
      float frequency = 1;
      
      System.Random prng = new System.Random(seed);
      Vector2[] octaveOffsets = new Vector2[octaves];

      // Set the octave offests
      for (int i = 0; i < octaves; i++)
      {
         float offsetX = prng.Next(-100000, 100000) + offset.x;
         float offsetY = prng.Next(-100000, 100000) - offset.y;
         octaveOffsets[i] = new Vector2(offsetX, offsetY);

         maxPossibleHeight += amplitude;
         amplitude *= persistence; 
      }
      
      float maxLocalNoiseHeight = float.MinValue;
      float minLocalNoiseHeight = float.MaxValue;

      float halfWidth = mapWidth / 2f;
      float halfHeight = mapHeight / 2f;
      
      if (scale <= 0)
         scale = 0.001f;
      
      // Generate noise values
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            amplitude = 1;
            frequency = 1;
            
            float noiseHeight = 0;
            
            for (int i = 0; i < octaves; i++)
            {
               // Sampling the X and Y values
               float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
               float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)  * 2 - 1; // Range[-2, 2]
               noiseHeight += perlinValue * amplitude;
               
               amplitude *= persistence;
               frequency *= lacunarity;
            }
            
            // Used for Normalising
            if(noiseHeight > maxLocalNoiseHeight)
               maxLocalNoiseHeight = noiseHeight;
            else if (noiseHeight < minLocalNoiseHeight)
               minLocalNoiseHeight = noiseHeight;

            noiseMap[x, y] = noiseHeight;
         }
      }

      // Normalise noiseMap
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            if (normalizeMode == NormalizeMode.Local)
            {
               noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x,y]);
            }
            else
            {
               // estimate the noise values
               float normalizedHeight = (noiseMap[x,y] + 1) / (maxPossibleHeight);
               noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
            }
         }
      }
      

      return noiseMap;
   }

   public static float[,] GenerateFBMNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves,
      float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
   {
      System.Random prng = new System.Random(seed);
      Vector2[] octaveOffsets = new Vector2[octaves];
      
      float maxPossibleHeight = 0;
      float amplitude = 1;
      float frequency = 1;

      for (int i = 0; i < octaves; i++)
      {
         float offsetX = prng.Next(-100000, 100000) + offset.x;
         float offsetY = prng.Next(-100000, 100000) - offset.y;
         octaveOffsets[i] = new Vector2(offsetX, offsetY);

         maxPossibleHeight += amplitude;
         amplitude *= persistence;
      }
      
      float[,] noiseMap = new float[mapWidth, mapHeight];

      float maxLocalNoiseHeight = float.MinValue;
      float minLocalNoiseHeight = float.MaxValue;
      
      float halfWidth = mapWidth / 2f;
      float halfHeight = mapHeight / 2f;

      // Set scale to a minimum of 0.001f (avoid /0 
      if (scale <= 0)
         scale = 0.001f;

      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            amplitude = 1;
            frequency = 1;
            float noiseHeight = 0;

            float max = 0;

            for (int i = 0; i < octaves; i++)
            {
               float sampleX = (x-halfWidth + octaveOffsets[i].x) / scale * frequency;
               float sampleY = (y-halfHeight + octaveOffsets[i].y) / scale * frequency;
               
               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * frequency * amplitude;
               max *= amplitude;
               
               noiseHeight += max* Mathf.Abs(perlinValue);
               
               amplitude *= persistence;
               frequency *= lacunarity;
            }

            noiseHeight /= amplitude;
            
            if(noiseHeight > maxLocalNoiseHeight)
               maxLocalNoiseHeight = noiseHeight;
            else if (noiseHeight < minLocalNoiseHeight)
               minLocalNoiseHeight = noiseHeight;
            
            noiseMap[x, y] = noiseHeight;
            
         }
      }

      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            if (normalizeMode == NormalizeMode.Local)
            {
               noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
            }
            else
            {
               float normalizedHeight = (noiseMap[x, y]) / (frequency * maxPossibleHeight) * 1.25f;
               noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
            }
         }
      }
      return noiseMap;
   }

   // Based on the Noise Map
   public static float[,] GenerateRidgeNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves,
      float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
   {
      float [,] noiseMap = new float[mapWidth, mapHeight];

      float maxPossibleHeight = 0;
      float amplitude = 1;
      float frequency = 1;
      
      System.Random prng = new System.Random(seed);
      Vector2[] octaveOffsets = new Vector2[octaves];

      // Set the octave offests
      for (int i = 0; i < octaves; i++)
      {
         float offsetX = prng.Next(-100000, 100000) + offset.x;
         float offsetY = prng.Next(-100000, 100000) - offset.y;
         octaveOffsets[i] = new Vector2(offsetX, offsetY);

         maxPossibleHeight += amplitude;
         amplitude *= persistence; 
      }
      
      float maxLocalNoiseHeight = float.MinValue;
      float minLocalNoiseHeight = float.MaxValue;

      float halfWidth = mapWidth / 2f;
      float halfHeight = mapHeight / 2f;
      
      if (scale <= 0)
         scale = 0.001f;
      
      // Generate noise values
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            amplitude = 1;
            frequency = 1;
            
            float noiseHeight = 0;
            
            for (int i = 0; i < octaves; i++)
            {
               // Sampling the X and Y values
               float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
               float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)  * 2 - 1; // Range[-2, 2]
               noiseHeight += perlinValue * amplitude;
               
               amplitude *= persistence;
               frequency *= lacunarity;
            }
            
            // Used for Normalising
            if(noiseHeight > maxLocalNoiseHeight)
               maxLocalNoiseHeight = noiseHeight;
            else if (noiseHeight < minLocalNoiseHeight)
               minLocalNoiseHeight = noiseHeight;

            noiseHeight = 1 - Mathf.Abs(noiseHeight);
            
            noiseMap[x, y] = noiseHeight;
         }
      }

      // Normalise noiseMap
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            if (normalizeMode == NormalizeMode.Local)
            {
               noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x,y]);
            }
            else
            {
               // estimate the noise values
               float normalizedHeight = (noiseMap[x,y] + 1) / (maxPossibleHeight);
               noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
            }
         }
      }
      return noiseMap;
   }

   public static float[,] GenerateDomainWarpedNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int warpScale, float warpStrength, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
   {
      float[,] warpedMap = new float[mapWidth, mapHeight];
      
      float[,] warpX = GenerateNoiseMap(mapWidth, mapHeight, seed * seed, warpScale, octaves, persistence, lacunarity, offset, normalizeMode);
      float[,] warpY = GenerateNoiseMap(mapWidth, mapHeight, seed + seed, warpScale, octaves, persistence, lacunarity, offset, normalizeMode);

      float halfWidth = mapWidth * 0.5f;
      float halfHeight = mapHeight * 0.5f;
      
      if(scale <= 0)
         scale = 0.001f;
      
      float maxH = float.MinValue;
      float minH = float.MaxValue;

      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            float dx = (warpX[x, y] * 2f - 1f) * warpStrength;
            float dy = (warpY[x, y] * 2f - 1f) * warpStrength;

            float amplitude = 1f, frequency = 1f;
            float height = 0f;

            for (int i = 0; i < octaves; i++)
            {
               float sampleX = (x - halfWidth + dx + offset.x) / scale * frequency;
               float sampleY = (y - halfWidth + dy + offset.y) / scale * frequency;
               
               
               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
               height += perlinValue * amplitude;
               amplitude *= persistence;
               frequency *= lacunarity;
            }
            
            if(height > maxH)
               maxH = height;
            if(height < minH)
               minH = height;
            
            warpedMap[x, y] = height;
         }
      }

      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            warpedMap[x,y] = Mathf.InverseLerp(minH, maxH, warpedMap[x,y]);
         }
      }

      
      return warpedMap;
   }

   public static float[,] GenerateTemperatureMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode, int tempAtSea, int tempAtSummit)
   {
      float [,] temperatureMap = new float[mapWidth, mapHeight];

      float maxPossibleTemperature = 0;
      float amplitude = 1;
      float frequency = 1;
      
      System.Random prng = new System.Random(seed);
      Vector2[] octaveOffsets = new Vector2[octaves];

      // Set the octave offests
      for (int i = 0; i < octaves; i++)
      {
         float offsetX = prng.Next(-100000, 100000) + offset.x;
         float offsetY = prng.Next(-100000, 100000) - offset.y;
         octaveOffsets[i] = new Vector2(offsetX, offsetY);

         maxPossibleTemperature += amplitude;
         amplitude *= persistence; 
      }
      
      float maxLocalTemp = float.MinValue;
      float minLocalTemp = float.MaxValue;

      float halfWidth = mapWidth / 2f;
      float halfHeight = mapHeight / 2f;
      
      if (scale <= 0)
         scale = 0.001f;
      
      // Generate noise values
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            amplitude = 1;
            frequency = 1;
            
            float temperature = 0;
            
            for (int i = 0; i < octaves; i++)
            {
               // Sampling the X and Y values
               float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;
               float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)  * tempAtSea - tempAtSummit; // Range[tempAtSummit, tempAtSea]
               temperature += perlinValue * amplitude;
               
               amplitude *= persistence;
               frequency *= lacunarity;
            }
            
            // Used for Normalising
            if(temperature > maxLocalTemp)
               maxLocalTemp = temperature;
            else if (temperature < minLocalTemp)
               minLocalTemp = temperature;

            temperatureMap[x, y] = temperature;
         }
      }

      // Normalise noiseMap
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            if (normalizeMode == NormalizeMode.Local)
            {
               //temperatureMap[x,y] = Mathf.InverseLerp(tempAtSummit, tempAtSea, temperatureMap[x,y]);
               temperatureMap[x,y] = Mathf.InverseLerp(minLocalTemp, maxLocalTemp, temperatureMap[x,y]);
            }
            else
            {
               //float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)  * tempAtSea - tempAtSummit; // Range[tempAtSummit, tempAtSea]

               
               // estimate the noise values
               float normalizedTemp = (temperatureMap[x,y] + tempAtSummit) / (maxPossibleTemperature * tempAtSea);
               temperatureMap[x, y] = Mathf.Clamp(normalizedTemp, -1, 1);
            }
         }
      }
      

      return temperatureMap;
   }
   
}
