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
   
   public static float[,] GeneratePerlinNoiseMap(int mapWidth, int mapHeight, int seed, float scale, Vector2 offset, NormalizeMode normalizeMode)
   {
      float [,] noiseMap = new float[mapWidth, mapHeight];

      float maxPossibleHeight = 0;
      float amplitude = 1;
      float frequency = 1;
      
      System.Random prng = new System.Random(seed);
      
      float offsetX = prng.Next(-100000, 100000) + offset.x;
      float offsetY = prng.Next(-100000, 100000) - offset.y;
      
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

            // Sampling the X and Y values
            float sampleX = (x-halfWidth + offsetX) / scale * frequency;
            float sampleY = (y-halfHeight + offsetY) / scale * frequency;

            float noiseHeight = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; // Range[-2, 2]
            
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
            noiseMap[x,y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x,y]);
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
      float maxPossibleAmplitude = 0;
      float amplitude = 1;
      float frequency = 1;
      
      for (int i = 0; i < octaves; i++)
      {
         float offsetX = prng.Next(-100000, 100000) + offset.x;
         float offsetY = prng.Next(-100000, 100000) - offset.y;
         octaveOffsets[i] = new Vector2(offsetX, offsetY);

         // Used for normalization
         maxPossibleHeight += amplitude;
         amplitude *= persistence;
         frequency *= lacunarity;
         
         // Used for normalization
         maxPossibleAmplitude += amplitude;
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
               max += amplitude;
               
               noiseHeight += amplitude * perlinValue;
               
               amplitude *= persistence;
               frequency *= lacunarity;
            }

            noiseHeight /= max;
            
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
               float normalizedHeight = (noiseMap[x, y]) / (maxPossibleAmplitude * maxPossibleHeight * 1.25f);
               //noiseMap[x, y] = Mathf.Clamp(normalizedHeight, 0, int.MaxValue);
               noiseMap[x,y] *= noiseMap[x, y] * 2f; // increase contrast
               noiseMap[x,y] = Mathf.Clamp01(noiseMap[x, y]); // Clamp back to range[0,1]
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
               float normalizedHeight = (noiseMap[x,y] * 2f) / (maxPossibleHeight);
               noiseMap[x, y] = Mathf.Clamp01(normalizedHeight);
            }
         }
      }
      return noiseMap;
   }

   // Domain Warping
   public static float[,] GenerateDomainWarpedNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int warpScale, float warpStrength, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
   {
      // Create final map
      float[,] warpedMap = new float[mapWidth, mapHeight];
      
      // Create two different noise maps for mixing (domain warping based on Perlin Noise)
      float[,] warpX = GenerateFBMNoiseMap(mapWidth, mapHeight, seed * seed, warpScale, octaves, persistence, lacunarity, offset, normalizeMode);
      float[,] warpY = GenerateRidgeNoiseMap(mapWidth, mapHeight, seed + seed, warpScale, octaves, persistence, lacunarity, offset, normalizeMode);
      
      // Get the center of the map (chunk)
      float halfWidth = mapWidth * 0.5f;
      float halfHeight = mapHeight * 0.5f;
      
      // Make sure scale is never 0
      if(scale <= 0)
         scale = 0.001f;
      
      // Values for maximum and minimum height to be used for normalization
      float maxH = float.MinValue;
      float minH = float.MaxValue;
      
      // Loop through the map
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            
            
            // 
            float dx = (warpX[x, y] * 2f - 1f) * warpStrength;
            float dy = (warpY[x, y] * 2f - 1f) * warpStrength;

            // Define Amplitude, Frequency
            float amplitude = 1f, frequency = 1f;
            
            // Height at point in map
            float height = 0f;

            // Loop through the octaves
            for (int i = 0; i < octaves; i++)
            {
               // Get a sample at location [x,y]
               float sampleX = (x - halfWidth + dx + offset.x) / scale * frequency;
               sampleX = 1 - sampleX;
               float sampleY = (y - halfHeight + dy + offset.y) / scale * frequency;
               
               // Generate perlin noise
               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
               
               // Create a height based on the float value of Perlin and multiply it by amplitude
               // Multiply Amplitude by Persistence (each loop amplitude becomes smaller and frequency becomes higher
               height += perlinValue * amplitude;
               amplitude *= persistence;
               frequency *= lacunarity;
            }
            
            // Compare and set maximum and minimum heights
            if(height > maxH)
               maxH = height;
            if(height < minH)
               minH = height;
            
            // Final value at position [x, y]
            warpedMap[x, y] = height;
         }
      }

      // Loop again and lerp the warped value at position [x, y] between the maximum and minimum heights
      // Normalize warpedMap
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            warpedMap[x,y] = Mathf.InverseLerp(minH, maxH, warpedMap[x,y]);
         }
         
      }
      
      return warpedMap;
   }

   // Generate Temperature Map based on Perlin Noise
   public static float[,] GenerateTemperatureMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistence, float lacunarity, Vector2 offset, NormalizeMode normalizeMode, int tempAtSea, int tempAtSummit)
   {
      float [,] temperatureMap = new float[mapWidth, mapHeight];
      float warpStrength = 30;
      int warpScale = 100;

      // 
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
         frequency *= lacunarity;
      }
      
      float maxLocalTemp = float.MinValue;
      float minLocalTemp = float.MaxValue;

      float halfWidth = mapWidth / 2f;
      float halfHeight = mapHeight / 2f;
      
      if (scale <= 0)
         scale = 0.001f;
      
      float[,] warpNoise = GenerateDomainWarpedNoiseMap(mapWidth, mapHeight, seed, scale, warpScale, warpStrength, octaves, persistence, lacunarity, offset, normalizeMode);
      
      // Generate noise values
      for (int y = 0; y < mapHeight; y++)
      {
         for (int x = 0; x < mapWidth; x++)
         {
            amplitude = 1;
            frequency = 1;
            
            float temperature = 0;
            
            // Range[tempAtSummit, tempAtSea]
            float dx = (warpNoise[x, y] * tempAtSea - tempAtSummit) * warpStrength * 0.25f;
            float dy = (warpNoise[x, y] * tempAtSea - tempAtSummit) * warpStrength * 0.25f;
            
            for (int i = 0; i < octaves; i++)
            {
               // Sampling the X and Y values
               float sampleX = (x - halfWidth + dx + offset.x) / scale * frequency;
               sampleX = 1 - sampleX;
               float sampleY = (y - halfHeight + dy + offset.y) / scale * frequency;

               float perlinValue = Mathf.PerlinNoise(sampleX, sampleY)  * tempAtSea - tempAtSummit; 
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
            //temperatureMap[x,y] = Mathf.InverseLerp(tempAtSummit, tempAtSea, temperatureMap[x,y]);
            temperatureMap[x,y] = Mathf.InverseLerp(minLocalTemp, maxLocalTemp, temperatureMap[x,y]);
         }
      }
      
      return temperatureMap;
   }

   public static float[,] GenerateVoronoiNoiseMap(int mapWidth, int mapHeight)
   {
      float[,] voronoiMap = new float[mapWidth, mapHeight];

      int gridSize = 10;
      
      
      return voronoiMap;
   }
   
}
