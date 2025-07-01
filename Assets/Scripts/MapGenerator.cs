using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEditor;

public class MapGenerator : MonoBehaviour
{
   public enum DrawMode
   {
      Noise,
      ColourMap,
      DrawMesh,
      Temperature,
      Humidity,
   }

   public enum NoiseType
   {
      Perlin,
      FBM,
      RidgeNoise,
      Temperature,
      Humidity,
   }
   
   [Header("Draw Mode")]
   public DrawMode drawMode;
   
   [Header("Map Settings")]
   public const int mapChunkSize = 241;
   public float heightMultiplier;
   public AnimationCurve heightCurve;
   public int editorPreviewLOD;
   
   [Header("Temperature Settings")]
   public int tempAtSea;
   public int tempAtSummit;
   
   [Header("Noise Settings")]
   public NoiseType noiseType;
   public float noiseScale;
   public int octaves;
   [Range(0, 1)]
   public float persistence;
   public float lacunarity;
   public int seed;
   public Vector2 offset;

   [Header("Domain Warping")] 
   public int warpScale;
   public float warpStrength;
   
   public Noise.NormalizeMode normalizeMode;
   
   [Header("Terrain Types")]
   public TerrainType[] regions;

   [Header("Biomes")]
   public SO_Biome[] biomes;
   private List<SO_Biome> sortedBiomes = new List<SO_Biome>();    
   
   [Header("Auto Update")]
   public bool autoUpdate;
   
   
   // Thread Queues
   Queue<MapThreadInfo<MapData>> mapDataThreadInfoQueue = new Queue<MapThreadInfo<MapData>>();
   Queue<MapThreadInfo<MeshData>> meshDataThreadInfoQueue = new Queue<MapThreadInfo<MeshData>>();

   private void Start()
   {
      // Sorts biomes on start by height
      sortedBiomes = biomes
         .OrderBy(b => b.minimumHeight)
         .ToList();
   }

   public void DrawMapInEditor()
   {
      MapData mapData= GenerateMapData(Vector2.zero);
      
      MapDisplay mapDisplay = FindFirstObjectByType<MapDisplay>();
      
      if(drawMode == DrawMode.Noise)
         mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
      else if (drawMode == DrawMode.ColourMap)
         mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colourMap, mapChunkSize, mapChunkSize));
      else if(drawMode == DrawMode.DrawMesh)
         //mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromTemperatureMap(mapData.temperatureMap));
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromColorMap(mapData.colourMap, mapChunkSize, mapChunkSize));
      else if(drawMode == DrawMode.Temperature)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromHeightMap(mapData.temperatureMap));
      else if(drawMode == DrawMode.Humidity)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromHeightMap(mapData.humidityMap));
   }
   

   private MapData GenerateMapData(Vector2 centre)
   {
      // Create new noise maps
      float[,] heightMap = new float[mapChunkSize, mapChunkSize];

      // Populate heightMap based on the type of noise chosen
      if (noiseType == NoiseType.Perlin || noiseType == NoiseType.Temperature)
         heightMap = Noise.GenerateNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, centre+offset, normalizeMode);
      else if (noiseType == NoiseType.FBM)
         heightMap = Noise.GenerateFBMNoiseMap(mapChunkSize, mapChunkSize , seed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
      else if (noiseType == NoiseType.RidgeNoise)
         heightMap = Noise.GenerateRidgeNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
      
      float[,] temperatureMap = Noise.GenerateTemperatureMap(mapChunkSize, mapChunkSize, seed + seed, noiseScale * 0.5f, octaves, persistence, lacunarity, centre + offset, normalizeMode, tempAtSea, tempAtSummit);
      float[,] humidityMap = Noise.GenerateDomainWarpedNoiseMap(mapChunkSize, mapChunkSize, seed, noiseScale, warpScale, warpStrength, octaves, persistence, lacunarity, centre + offset, normalizeMode);
      
      
      Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
      
      for (int y = 0; y < mapChunkSize; y++)
      {
         for (int x = 0; x < mapChunkSize; x++)
         {
            //Original Code
            //Loop through the regions to see where the height falls into
            // float currentHeight = noiseMap[x, y];
            // for (int i = 0; i < regions.Length; i++)
            // {
            //    if (currentHeight >= regions[i].height)
            //    {
            //       colourMap[y * mapChunkSize + x] = regions[i].colour;
            //    }
            //    else
            //    {
            //       break;
            //    }
            // }
            
            
            //Trial with biome colours based on temperature
            // foreach (SO_Biome biome in sortedBiomes)
            // {
            //    // Calculate normalized distance along each axis
            //    float centerH = biome.minimumHeight + biome.heightSpread * 0.5f;
            //    float centerT = biome.minimumTemperature + biome.temperatureSpread * 0.5f;
            //    float dh = (currentHeight - centerH) / (biome.heightSpread * 0.5f);
            //    float dt = (currentTemperature - centerT) / (biome.temperatureSpread * 0.5f);
            //    
            //    float dist2 = dh*dh + dt*dt;
            //    if (dist2 < bestDist)
            //    {
            //       bestDist = dist2;
            //       colour = biome.colour;
            //    }
            //    
            //    // // Gaussian-like weight 
            //    // float weight = 1f / (dh * dh + dt * dt + 0.0001f);
            //    //
            //    // if (weight > bestWeight)
            //    // {
            //    //    bestWeight = weight;
            //    //    colour = biome.colour;
            //    // }
            //    
            // }
            // colourMap[y * mapChunkSize + x] = colour;


            // Default values
            float bestDist2 = float.MaxValue;
            Color bestColour = Color.magenta;

            // Get the value of each of the noise maps in the current position per chunk
            float currenHeight = heightMap[x, y];
            float currentTemperature = temperatureMap[x, y];
            float currentHumidity = humidityMap[x, y];

            foreach (SO_Biome biome in sortedBiomes)
            {
               float d2 =  Dist2ToRange(currentHumidity, biome.minimumHumidity, biome.maximumHumidity); // sample humidity
               d2 += Dist2ToRange(currentTemperature, biome.minimumTemperature, biome.maximumTemperature); // sample temperature
               
               // sample height with a multiplier as the height is the most decisive factor
               // without this we could get high biomes spawning in low areas
               d2 += Dist2ToRange(currenHeight, biome.minimumHeight, biome.maximumHeight) * 5f; 
               
               // remove the bias for each biome
               d2 -= biome.weightBias;
               
               // check which biome is most suited.
               if(d2 < bestDist2)
               {
                  bestDist2 = d2;
                  bestColour = biome.colour;
               }
            }

            colourMap[y * mapChunkSize + x] = bestColour;
         }
      }

      return new MapData(heightMap, colourMap, temperatureMap, humidityMap);
   }

   void Update()
   {
      // Dequeu threads
      if (mapDataThreadInfoQueue.Count > 0)
      {
         for (int i = 0; i < mapDataThreadInfoQueue.Count; i++)
         {
            MapThreadInfo<MapData> threadInfo = mapDataThreadInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
         }
      }

      if (meshDataThreadInfoQueue.Count > 0)
      {
         for (int i = 0; i < meshDataThreadInfoQueue.Count; i++)
         {
            MapThreadInfo<MeshData> threadInfo = meshDataThreadInfoQueue.Dequeue();
            threadInfo.callback(threadInfo.parameter);
         }
      }
   }
   
#region Threading

   // Start MapData Thread
   public void RequestMapData(Vector2 centre, Action<MapData> callback)
   {
      ThreadStart threadStart = delegate
      {
         MapDataThread(centre, callback);
      };
      new Thread(threadStart).Start();
   }

   // Gets MapData and adds the callback to the queue
   void MapDataThread(Vector2 centre, Action<MapData> callback)
   { 
      MapData mapData = GenerateMapData(centre);

      
      // Locked so it can't be accessed simultaneously
      lock (mapDataThreadInfoQueue)
      {
         mapDataThreadInfoQueue.Enqueue(new MapThreadInfo<MapData>(callback, mapData));
      }
   }
   
   public void RequestMeshData(MapData mapData, int lod, Action<MeshData> callback)
   {
      ThreadStart threadStart = delegate
      {
         MeshDataThread(mapData, lod, callback);
      };
      new Thread(threadStart).Start();
   }

   // Gets MapData and adds the callback to the queue
   void MeshDataThread(MapData mapData, int lod, Action<MeshData> callback)
   { 
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, heightMultiplier, heightCurve, lod);
      
      // Locked so it can't be accessed simultaneously
      lock (meshDataThreadInfoQueue)
      {
         meshDataThreadInfoQueue.Enqueue(new MapThreadInfo<MeshData>(callback, meshData));
      }
   }
   
#endregion
   
   // Called automatically when editor settings are changed
#if UNITY_EDITOR
   private void OnValidate()
   {
      // sanity‐clamp your noise params
      if (lacunarity < 1)
         lacunarity = 1;
      if (octaves < 0)    
         octaves   = 0;
      
      // Organize
      sortedBiomes = biomes
         .OrderBy(b => b.minimumHeight)
         .ThenBy(b => b.minimumTemperature)
         .ToList();
   }
#endif
   
   // Hold the info for the threads, kept generic to be used for the mesh data and map data
   struct MapThreadInfo<T>
   {
       public readonly Action<T> callback;
       public readonly T parameter;

       public MapThreadInfo(Action<T> callback, T parameter)
       {
          this.callback = callback;
          this.parameter = parameter;
       }
   }
   
   //Helper Function for distance squared
   private static float Dist2ToRange(float value, float minimum, float maximum)
   {
      if (value < minimum)
         return (minimum - value) * (minimum - value);
      else if (value > maximum)
         return (maximum - value) * (maximum - value);
      else
         return 0f;
   }
}

[System.Serializable]
public struct TerrainType
{
   public string name;
   public float height;
   public Color colour;
}

public struct MapData
{
   public readonly float[,] heightMap;
   public readonly Color[] colourMap;
   public readonly float[,] temperatureMap;
   public readonly float[,] humidityMap;

   public MapData(float[,] heightMap, Color[] colourMap)
   {
      this.heightMap = heightMap;
      this.colourMap = colourMap;
      this.temperatureMap = new float[0, 0];
      this.temperatureMap[0,0] = 0;
      this.humidityMap = new float[0, 0];
      this.humidityMap[0,0] = 0;
   }

   public MapData(float[,] heightMap, Color[] colourMap, float[,] temperatureMap)
   {
      this.heightMap = heightMap;
      this.colourMap = colourMap;
      this.temperatureMap = temperatureMap;
      this.humidityMap = new float[0, 0];
      this.humidityMap[0,0] = 0;
   }
   
   public MapData(float[,] heightMap, Color[] colourMap, float[,] temperatureMap, float[,] humidityMap)
   {
      this.heightMap = heightMap;
      this.colourMap = colourMap;
      this.temperatureMap = temperatureMap;
      this.humidityMap = humidityMap;
   }
   
}
