using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using Structs;
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
      //Voronoi,
   }

   // Enum for choosing which type of noise is used for creating terrain

   [Header("Testing")] 
   public GameObject plane;
   public GameObject mesh;
   
   [Header("Draw Mode")]
   public DrawMode drawMode;
   
   [Header("Map Settings")]
   public const int mapChunkSize = 241;
   public int editorPreviewLOD;
   public float drawHeightMultiplier;
   public NormalizeMode normalizeMode;
   [Range(0,1)]public float ridgesIntensity;
   
   [Header("Height Map Settings")]
   public NoiseSettings HeightSettings;
   [Range(0,1)]public float heightWeight;
   public AnimationCurve heightCurve;
   
   [Header("Temperature Map Settings")]
   public NoiseSettings TemperatureSettings;
   public AnimationCurve temperatureCurve;
   [Range(0,1)]public float temperatureWeight;
   public int tempAtSea;
   public int tempAtSummit;
   
   [Header("Humidity Map Settings")]
   public NoiseSettings HumiditySettings;
   [Range(0,1)]public float humidityWeight;
   public AnimationCurve humidityCurve;
   
   [Header("Domain Warping")] 
   public int warpScale;
   public float warpStrength;
   
   private Vector2Int[,] closestSites;
   private Vector2Int[,] secondClosestSites;
   private Vector2Int[,] sitePositions;
   
   [Header("Biomes")]
   public SO_Biome[] biomes;
   private List<SO_Biome> sortedBiomes = new List<SO_Biome>();    
   
   [Header("Auto Update")]
   public bool autoUpdate;
   
   // Biome Generator
   BiomeMapGenerator biomeMapGenerator;
   
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
         mapDisplay.DrawTexture(TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
      else if (drawMode == DrawMode.DrawMesh)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromColourMap(mapData.colourMap, mapChunkSize, mapChunkSize));
      else if (drawMode == DrawMode.Temperature)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromTemperature(mapData.temperatureMap));
      else if (drawMode == DrawMode.Humidity)
         mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD), TextureGenerator.TextureFromHumidity(mapData.humidityMap));
      // else if(drawMode == DrawMode.Voronoi)
      //  mapDisplay.DrawMesh(MeshGenerator.GenerateTerrainMesh(mapData.heightMap, drawHeightMultiplier, heightCurve, editorPreviewLOD),TextureGenerator.TextureFromVoronoi(mapData.heightMap, mapChunkSize, closestSites));
   }

   
   
   private MapData GenerateMapData(Vector2 centre)
   {
      // Create new noise maps
      
      // Populate heightMap based on the type of noise chosen
     // float[,] heightMap = NoiseMapGenerator.GenerateNoiseMap()
      
//       
//       float[,] heightMap = GenerateNoiseMap(heightNoiseType, heightSeed, noiseScale, Vector2.zero);
//       float[,] temperatureMap = GenerateNoiseMap(temperatureNoiseType, temperatureSeed, noiseScale, Vector2.zero);
//       float[,] humidityMap = GenerateNoiseMap(humidityNoiseType, humiditySeed, noiseScale, Vector2.zero);
//       
//       float[,] ridgesMap = Noise.GenerateRidgeNoiseMap(mapChunkSize, mapChunkSize, heightSeed, noiseScale, octaves, persistence, lacunarity, centre + offset, normalizeMode);
//       
//       for (int y = 0; y < heightMap.GetLength(0); y++)
//       {
//          for (int x = 0; x < heightMap.GetLength(1); x++)
//          {
//             ridgesMap[x, y] *= ridgesIntensity;
//             heightMap[x, y] = Mathf.Lerp(heightMap[x, y], ridgesMap[x, y], 0.1f);
//             
//          }
//       }
//
//       // Re-normalize height map 
//       heightMap = Noise.Normalize(heightMap);
//
//       /* Original Biome Logic
//       
//       Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
//       
//       for (int y = 0; y < mapChunkSize; y++)
//       {
//          for (int x = 0; x < mapChunkSize; x++)
//          {
//             // Default values
//             float bestDist2 = float.MaxValue;
//             Color bestColour = Color.magenta;
//
//             // Get the value of each of the noise maps in the current position per chunk
//             float currenHeight = heightMap[x, y];
//             float currentTemperature = temperatureMap[x, y];
//             float currentHumidity = humidityMap[x, y];
//
//             foreach (SO_Biome biome in sortedBiomes)
//             {
//                float d2 = Dist2ToRange(currentHumidity, biome.minimumHumidity, biome.maximumHumidity) + humidityWeight;
//                d2 *= humidityCurve.Evaluate(d2); // sample humidity
//                d2 += Dist2ToRange(currentTemperature, biome.minimumTemperature, biome.maximumTemperature) * temperatureWeight + temperatureCurve.Evaluate(d2); // sample temperature
//                
//                // sample height with a multiplier as the height is the most decisive factor
//                // without this we could get high biomes spawning in low areas
//                d2 += Dist2ToRange(currenHeight, biome.minimumHeight, biome.maximumHeight) * heightWeight + heightCurve.Evaluate(d2); 
//                
//                // remove the bias for each biome
//                d2 -= biome.weightBias;
//                
//                // check which biome is most suited.
//                if(d2 < bestDist2)
//                {
//                   bestDist2 = d2;
//                   bestColour = biome.colour;
//                }
//             }
//
//             colourMap[y * mapChunkSize + x] = bestColour;
//          }
//       }
//       */
//
//
//       SO_Biome[,] biomeMap = new SO_Biome[mapChunkSize,mapChunkSize];
//       
//       // Evaluate biomes
//       BiomeEvaluator biomeEvaluator = new BiomeEvaluator(sortedBiomes);
//
//       for (int y = 0; y < mapChunkSize; y++)
//       {
//          for (int x = 0; x < mapChunkSize; x++)
//          {
//             float height = heightMap[x, y];
//             float temperature = temperatureMap[x, y];
//             float humidity = humidityMap[x, y];
//
//             SO_Biome selectedBiome = biomeEvaluator.Evaluate(height, temperature, humidity);
//             biomeMap[x, y] = selectedBiome;
//
//          }
//       }
//       
//       
//       // create color based on the suitability calculated on the previous passes
//       Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
//       for(int y = 0; y < mapChunkSize; y++)
//          for(int x = 0; x < mapChunkSize; x++)
//             colourMap[y * mapChunkSize + x] = biomeMap[x, y].colour;
      biomeMapGenerator = new BiomeMapGenerator(sortedBiomes);

      // Create noise maps and populate them
      float[,] rawHeightMap = NoiseMapGenerator.GenerateNoiseMap(HeightSettings, centre, normalizeMode, mapChunkSize, mapChunkSize);
      float[,] rawTemperatureMap = NoiseMapGenerator.GenerateNoiseMap(TemperatureSettings, centre, normalizeMode, mapChunkSize, mapChunkSize);
      float[,] rawHumidityMap = NoiseMapGenerator.GenerateNoiseMap(HumiditySettings, centre, normalizeMode, mapChunkSize, mapChunkSize);
      
      // Create final noisemaps
      float[,] heightMap = new float[mapChunkSize, mapChunkSize];
      float[,] temperatureMap = new float[mapChunkSize, mapChunkSize];
      float[,] humidityMap = new float[mapChunkSize, mapChunkSize];

      // Evaluate each noiseMap with their specific curve
      for (int y = 0; y < mapChunkSize; y++)
      {
         for (int x = 0; x < mapChunkSize; x++)
         {
            heightMap[x, y] = heightCurve.Evaluate(rawHeightMap[x, y]);
            temperatureMap[x, y] = temperatureCurve.Evaluate(rawTemperatureMap[x, y]);
            humidityMap[x, y] = humidityCurve.Evaluate(rawHumidityMap[x, y]);
         }
      }
      
      // Create the colour map for the biome colours
      Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
      
      // Combine into a biome grid
      SO_Biome[,] biomeMap = biomeMapGenerator.GenerateBiomeMap(heightMap, temperatureMap, humidityMap);
      
      // Generate colour based on the best biomes
      colourMap = ColourMapGenerator.GenerateColourMap(biomeMap);
      
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
