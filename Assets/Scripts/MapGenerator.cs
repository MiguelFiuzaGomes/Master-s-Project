using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using Unity.PlasticSCM.Editor.WebApi;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine.Serialization;

public class MapGenerator : MonoBehaviour
{
   // Enum for choosing what drawMode is used for testing
   public enum DrawMode
   {
      Noise,
      ColourMap,
      DrawMesh,
      Temperature,
      Humidity,
      Voronoi,
   }

   // Enum for choosing which type of noise is used for creating terrain
   public enum NoiseType
   {
      Perlin,
      FBM,
      RidgeNoise,
      DomainWarping,
      Voronoi,
   }

   [Header("Testing")] 
   public GameObject plane;
   public GameObject mesh;
   
   [Header("Draw Mode")]
   public DrawMode drawMode;
   
   [Header("Map Settings")]
   public const int mapChunkSize = 241;
   public int editorPreviewLOD;
   public float drawHeightMultiplier;
   public Noise.NormalizeMode normalizeMode;
   [Range(0,1)]public float ridgesIntensity;
   
   [Header("Height Map Settings")]
   public NoiseType heightNoiseType;
   public int heightSeed;
   [Range(0,1)]public float heightWeight;
   public AnimationCurve heightCurve;
   
   [Header("Temperature Map Settings")]
   public int temperatureSeed;
   public NoiseType temperatureNoiseType;
   public AnimationCurve temperatureCurve;
   [Range(0,1)]public float temperatureWeight;
   public int tempAtSea;
   public int tempAtSummit;
   
   [Header("Humidity Map Settings")]
   public int humiditySeed;
   public NoiseType humidityNoiseType;
   [Range(0,1)]public float humidityWeight;
   public AnimationCurve humidityCurve;
   
   [Header("Noise Settings")]
   public float noiseScale;
   public int octaves;
   [Range(0, 1)] public float persistence;
   public float lacunarity;
   public Vector2 offset;

   [Header("Domain Warping")] 
   public int warpScale;
   public float warpStrength;
   
   [Header("Voronoi Settings")]
   public int siteCount;
   public int gridSize;

   private Vector2Int[,] sites;
   
   
   
   //[Header("Terrain Types")]
   //public TerrainType[] regions;

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

   // Testing purposes for in editor
   public void DrawMapInEditor()
   {
      MapData mapData = GenerateMapData(Vector2.zero);
      MapDisplay mapDisplay = FindFirstObjectByType<MapDisplay>();

      // Check which drawMode is selected and apply it to either a plane or a mesh
      if (drawMode == DrawMode.Noise)
         mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
      else if (drawMode == DrawMode.ColourMap)
         mapDisplay.DrawTexture(TextureGenerator.TextureFromColorMap(mapData.colourMap, mapChunkSize, mapChunkSize));
      else if (drawMode == DrawMode.DrawMesh)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromColorMap(mapData.colourMap, mapChunkSize, mapChunkSize));
      else if (drawMode == DrawMode.Temperature)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromTemperature(mapData.temperatureMap));
      else if (drawMode == DrawMode.Humidity)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromHumidity(mapData.humidityMap));
      else if(drawMode == DrawMode.Voronoi)
       mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD),TextureGenerator.TextureFromVoronoi(mapData.heightMap, mapChunkSize, sites));
   }

   private MapData GenerateMapData(Vector2 centre)
   {
      // Create new noise maps
      float[,] heightMap = new float[mapChunkSize, mapChunkSize];
      float[,] temperatureMap = new float[mapChunkSize, mapChunkSize];
      float[,] humidityMap = new float[mapChunkSize, mapChunkSize];
      float[,] ridgesMap = new float[mapChunkSize, mapChunkSize];
      float[,] voronoiMap = new float[mapChunkSize, mapChunkSize];

      // Populate heightMap based on the type of noise chosen
      switch (heightNoiseType)
      {
         case NoiseType.Perlin:
            heightMap = Noise.GeneratePerlinNoiseMap(mapChunkSize, mapChunkSize, heightSeed, noiseScale, centre+offset, normalizeMode);
            break;
         case NoiseType.FBM:
            heightMap = Noise.GenerateFBMNoiseMap(mapChunkSize, mapChunkSize , heightSeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.RidgeNoise:
            heightMap = Noise.GenerateRidgeNoiseMap(mapChunkSize, mapChunkSize, heightSeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.DomainWarping:
            heightMap = Noise.GenerateDomainWarpedNoiseMap(mapChunkSize, mapChunkSize, heightSeed, noiseScale, warpScale, warpStrength, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.Voronoi:
            heightMap = Noise.GenerateVoronoiNoiseMap(mapChunkSize, gridSize, heightSeed, ref sites);
            break;
         default:
            heightMap = Noise.GeneratePerlinNoiseMap(mapChunkSize, mapChunkSize, heightSeed, noiseScale, centre+offset, normalizeMode);
            break;
      }
      
      switch (temperatureNoiseType)
      {
         case NoiseType.Perlin:
            temperatureMap = Noise.GeneratePerlinNoiseMap(mapChunkSize, mapChunkSize, temperatureSeed, noiseScale, centre+offset, normalizeMode);
            break;
         case NoiseType.FBM:
            temperatureMap = Noise.GenerateFBMNoiseMap(mapChunkSize, mapChunkSize , temperatureSeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.RidgeNoise:
            temperatureMap = Noise.GenerateRidgeNoiseMap(mapChunkSize, mapChunkSize, temperatureSeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.DomainWarping:
            temperatureMap = Noise.GenerateDomainWarpedNoiseMap(mapChunkSize, mapChunkSize, temperatureSeed, noiseScale, warpScale, warpStrength, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.Voronoi:
            temperatureMap = Noise.GenerateVoronoiNoiseMap(mapChunkSize, gridSize, temperatureSeed, ref sites);
            break;
         default:
            temperatureMap = Noise.GeneratePerlinNoiseMap(mapChunkSize, mapChunkSize, temperatureSeed, noiseScale, centre+offset, normalizeMode);
            break;
      }
      
      switch (humidityNoiseType)
      {
         case NoiseType.Perlin:
            humidityMap = Noise.GeneratePerlinNoiseMap(mapChunkSize, mapChunkSize, humiditySeed, noiseScale, centre+offset, normalizeMode);
            break;
         case NoiseType.FBM:
            humidityMap = Noise.GenerateFBMNoiseMap(mapChunkSize, mapChunkSize , humiditySeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.RidgeNoise:
            humidityMap = Noise.GenerateRidgeNoiseMap(mapChunkSize, mapChunkSize, humiditySeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.DomainWarping:
            humidityMap = Noise.GenerateDomainWarpedNoiseMap(mapChunkSize, mapChunkSize, humiditySeed, noiseScale, warpScale, warpStrength, octaves, persistence, lacunarity, centre + offset, normalizeMode);
            break;
         case NoiseType.Voronoi:
            humidityMap = Noise.GenerateVoronoiNoiseMap(mapChunkSize, gridSize, humiditySeed, ref sites);
            break;
         default:
            humidityMap = Noise.GeneratePerlinNoiseMap(mapChunkSize, mapChunkSize, humiditySeed, noiseScale, centre+offset, normalizeMode);
            break;
      }
      
      ridgesMap = Noise.GenerateRidgeNoiseMap(mapChunkSize, mapChunkSize, heightSeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
      voronoiMap = Noise.GenerateVoronoiNoiseMap(mapChunkSize, gridSize, heightSeed, ref sites);
      
      for (int y = 0; y < heightMap.GetLength(0); y++)
      {
         for (int x = 0; x < heightMap.GetLength(1); x++)
         {
            ridgesMap[x, y] *= ridgesIntensity;
            voronoiMap[x, y] *= 0.5f;
            heightMap[x, y] = Mathf.Lerp(heightMap[x, y], ridgesMap[x, y], 0.1f);
            heightMap[x, y] = Mathf.Lerp(heightMap[x, y], voronoiMap[x, y], 0.1f);
         }
      }
      
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
            
            
            // Default values
            float bestDist2 = float.MaxValue;
            Color bestColour = Color.magenta;

            // Get the value of each of the noise maps in the current position per chunk
            float currenHeight = heightMap[x, y];
            float currentTemperature = temperatureMap[x, y];
            float currentHumidity = humidityMap[x, y];

            foreach (SO_Biome biome in sortedBiomes)
            {
               float d2 = Dist2ToRange(currentHumidity, biome.minimumHumidity, biome.maximumHumidity) + humidityWeight;
               d2 *= humidityCurve.Evaluate(d2); // sample humidity
               d2 += Dist2ToRange(currentTemperature, biome.minimumTemperature, biome.maximumTemperature) * temperatureWeight + temperatureCurve.Evaluate(d2); // sample temperature
               
               // sample height with a multiplier as the height is the most decisive factor
               // without this we could get high biomes spawning in low areas
               d2 += Dist2ToRange(currenHeight, biome.minimumHeight, biome.maximumHeight) * heightWeight + heightCurve.Evaluate(d2); 
               
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
      MeshData meshData = MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, lod);
      
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
         octaves = 0;
      if(gridSize <= 0)
         gridSize = 1;

      // Organize
      sortedBiomes = biomes
         .OrderBy(b => b.minimumHeight)
         .ThenBy(b => b.minimumTemperature)
         .ToList();

      if (!Application.isPlaying)
      {
         if (drawMode == DrawMode.Noise || drawMode == DrawMode.ColourMap)
         {
            plane.SetActive(true);
            mesh.SetActive(false);
         }
         else
         {
            plane.SetActive(false);
            mesh.SetActive(true);
         }
      }
      else
      {
         drawHeightMultiplier *= 0.1f;
         
         plane.SetActive(false);
         mesh.SetActive(false);
      }
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

#region structs
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

   // Original Code
   public MapData(float[,] heightMap, Color[] colourMap)
   {
      this.heightMap = heightMap;
      this.colourMap = colourMap;
      this.temperatureMap = new float[0, 0];
      this.temperatureMap[0,0] = 0;
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
#endregion
