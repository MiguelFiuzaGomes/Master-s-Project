using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Threading;
using Structs;

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

   // What noise to draw in Noise DrawMode
   public enum DrawNoiseType
   {
      Height,
      Temperature,
      Humidity,
      Ridges,
   }

   // Enum for choosing which type of noise is used for creating terrain

   [Header("Testing")] 
   public GameObject plane;
   public GameObject mesh;
   
   [Header("Draw Mode")]
   public DrawMode drawMode;
   public DrawNoiseType drawNoiseType;
   
   [Header("Map Settings")]
   public const int mapChunkSize = 241;
   public int editorPreviewLOD;
   public float drawHeightMultiplier;
   public NormalizeMode normalizeMode;
   [Range(1, 8), Tooltip("How many times the estimation size (256) is multiplied.")] public int estimationMultiplier;
   private int estimationSize = 256;
   
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
   
   // Hidden from the viewer
   // Used for lifting the hills
   [Header("Ridge Map Settings")] 
   [Range(0,1)]public float ridgesIntensity;
   [HideInInspector] public NoiseSettings RidgeSettings;
   [HideInInspector] public AnimationCurve ridgesCurve;
   
   
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

      estimationSize *= estimationMultiplier;
      
      // Calculate Padding used for domain warping normalization 
      HeightSettings.padding = Mathf.CeilToInt(HeightSettings.warpStrength / HeightSettings.warpScale) + 2;
      TemperatureSettings.padding = Mathf.CeilToInt(TemperatureSettings.warpStrength / TemperatureSettings.warpScale) + 2;
      HumiditySettings.padding = Mathf.CeilToInt(HumiditySettings.warpStrength / HumiditySettings.warpScale) + 2;

      // Attemp to get bounds for each noiseMap
      (HeightSettings.minVal, HeightSettings.maxVal) = NoiseMapGenerator.EstimateNoiseRange(HeightSettings, estimationSize, estimationSize);
      (TemperatureSettings.minVal, TemperatureSettings.maxVal) = NoiseMapGenerator.EstimateNoiseRange(TemperatureSettings, estimationSize, estimationSize);
      (HumiditySettings.minVal, HumiditySettings.maxVal) = NoiseMapGenerator.EstimateNoiseRange(HumiditySettings, estimationSize, estimationSize);
      
      // RidgesMap
      // Used for detailing height map
      // Inherits the values from HeightSettings
      RidgeSettings = HeightSettings;
      RidgeSettings.noiseType = NoiseType.Ridge;
      ridgesCurve = heightCurve;
   }

   // Testing purposes for in editor
   public void DrawMapInEditor()
   {
      MapData mapData = GenerateMapData(Vector2.zero);
      MapDisplay mapDisplay = FindFirstObjectByType<MapDisplay>();

      // Check which drawMode is selected and apply it to either a plane or a mesh
      if (drawMode == DrawMode.Noise)
      {
         switch (drawNoiseType)
         {
            case DrawNoiseType.Height:
               mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.heightMap));
               break;
            case DrawNoiseType.Humidity:
               mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.humidityMap));
               break;
            case DrawNoiseType.Temperature:
               mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.temperatureMap));
               break;
            case DrawNoiseType.Ridges:
               mapDisplay.DrawTexture(TextureGenerator.TextureFromHeightMap(mapData.ridgesMap));
               break;
         }

      }
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
      float[,] rawRidgesMap = NoiseMapGenerator.GenerateNoiseMap(RidgeSettings, centre, normalizeMode, mapChunkSize, mapChunkSize);
      
      // Create final noisemaps
      float[,] heightMap = new float[mapChunkSize, mapChunkSize];
      float[,] temperatureMap = new float[mapChunkSize, mapChunkSize];
      float[,] humidityMap = new float[mapChunkSize, mapChunkSize];
      float[,] ridgesMap = new float[mapChunkSize, mapChunkSize];
      
      // Clamp curves to [0,1]
      heightCurve.preWrapMode = WrapMode.Clamp;
      heightCurve.postWrapMode = WrapMode.Clamp;
      temperatureCurve.preWrapMode = WrapMode.Clamp;
      temperatureCurve.postWrapMode = WrapMode.Clamp;
      humidityCurve.preWrapMode = WrapMode.Clamp;
      humidityCurve.postWrapMode = WrapMode.Clamp;
      ridgesCurve.preWrapMode = WrapMode.Clamp;
      ridgesCurve.postWrapMode = WrapMode.Clamp;

      // Evaluate each noiseMap with their specific curve
      for (int y = 0; y < mapChunkSize; y++)
      {
         for (int x = 0; x < mapChunkSize; x++)
         {
            // safeguard against NaNs and Infinites
            // clamp inputs
            float rawHeight = rawHeightMap[x, y];
            if(!float.IsFinite(rawHeight)) rawHeight = 0;
            
            float ridge = rawRidgesMap[x, y];
            if(!float.IsFinite(ridge)) ridge = 0;
            
            float temperature = Mathf.Clamp01(rawTemperatureMap[x, y]); 
            if(!float.IsFinite(temperature)) temperature = 0;
            
            float humidity = Mathf.Clamp01(rawHumidityMap[x, y]);
            if(!float.IsFinite(humidity)) humidity = 0;
            
            // clamp the evaluated result 
            // Animation Curves can "overshoot" their values even when clamped in [0,1]
            // Safeguard against NaNs and Infinites
            
            float ridgesEvaluate = ridgesCurve.Evaluate(rawRidgesMap[x, y]) * ridgesIntensity;
            if(!float.IsFinite(ridgesEvaluate)) ridgesEvaluate = 0;
            
            float height = rawHeight + ridge * ridgesIntensity;
            var heightEvaluate = Mathf.SmoothStep(0f, 1f, height);
            
            if(!float.IsFinite(heightEvaluate)) heightEvaluate = 0;
            float temperatureEvaluate = temperatureCurve.Evaluate(temperature);
            if(!float.IsFinite(temperatureEvaluate)) temperatureEvaluate = 0;
            float humidityEvaluate = humidityCurve.Evaluate(humidity);
            if(!float.IsFinite(humidityEvaluate)) humidityEvaluate = 0;
            
            // Pass the clamped value to the final maps
            heightMap[x, y] = heightEvaluate;
            temperatureMap[x, y] = Mathf.Clamp01(temperatureEvaluate);
            humidityMap[x, y] = Mathf.Clamp01(humidityEvaluate);
            ridgesMap[x, y] = Mathf.Clamp01(ridgesEvaluate);
            
         }
      }

      // Create the colour map for the biome colours
      Color[] colourMap = new Color[mapChunkSize * mapChunkSize];
      
      // Combine into a biome grid
      SO_Biome[,] biomeMap = biomeMapGenerator.GenerateBiomeMap(heightMap, temperatureMap, humidityMap);
     
      // Carve beach biomes along the coast
      biomeMap = BeachGenerator.CarveBeach(
          biomeMap, 
          heightMap,
          shallows: sortedBiomes.First(b => b.name == "Shallows"), 
          beach: sortedBiomes.First(b => b.name == "Beach"),
          deepOcean: sortedBiomes.First(b => b.name == "Deep Ocean"));
      
      
      // Prune small islands so there isn't single tile biomes
      biomeMap = BiomePost.PruneTinyRegions(biomeMap, 6);
      
      // Generate colour based on the best biomes
      colourMap = ColourMapGenerator.GenerateColourMap(biomeMap);
      
      return new MapData(heightMap, colourMap, temperatureMap, humidityMap, ridgesMap);
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
      
      LogHeightStats(mapData.heightMap, "[Runtime] MapGenerator.MapDataThread: height map");
      
      SanitizeHeightMap(mapData.heightMap);
      
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
   
   // Helper function to Sanitize data
   private static void SanitizeHeightMap(float[,] heightMap)
   {
      int width = heightMap.GetLength(0), height = heightMap.GetLength(1);

      // Copy the original so we don't change values as we go
      float[,] original = heightMap.Clone() as float[,];
      
      // Offsets for the 8 neighbours
      /*
            (-1, -1) (0, -1) (1, -1)
            (-1, 0) (0, 0) (1, 0)
            (-1, 1) (0, 1) (1, 1)       
      */
      
      int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
      int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };
      
      
      for (int y = 0; y < height; y++)
      {
         for (int x = 0; x < width; x++)
         {
            float value = original[x, y];
            
            // Catch NaNs and infinites
            // Turn their height into the average of their neighbouring values
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
               // Gather all valid neighbours
               float sum = 0f;
               int count = 0;

               for (int i = 0; i < dx.Length; i++)
               {
                  // Define the neighbours
                  int nx = x + dx[i];
                  int ny = y + dy[i];
                  
                  // Exclude out of bounds
                  if(nx < 0 || nx >= width || ny < 0 || ny >= height)
                     continue;
                  
                  float neighbourValue = original[nx, ny];
                  
                  // Exclude invalid neighbours (NaN or infinite)
                  if(float.IsNaN(neighbourValue) || float.IsInfinity(neighbourValue))
                     continue;
                  
                  // Sum the value of the number
                  // Increase count
                  sum += neighbourValue;
                  count++; 
               }
               
               // Replace heightMap's value or fallback to 0 if there's no valid neighbours
               heightMap[x, y] = (count > 0 ? (sum / count) : 0f);
            }
            // Make sure that the value is unchanged
            else
               heightMap[x,y] = original[x, y];
         }
      }
   }
   
   // Debugging
   public static void LogHeightStats(float[,] map, string label)
   {
      int w = map.GetLength(0), h = map.GetLength(1);
      float min = float.MaxValue, max = float.MinValue;
      int nan = 0;
      for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
      {
         float v = map[x,y];
         if (float.IsNaN(v) || float.IsInfinity(v)) { nan++; continue; }
         min = Mathf.Min(min, v);
         max = Mathf.Max(max, v);
      }
      Debug.Log($"{label} → min={min:F3}, max={max:F3}, NaNs={nan}/{w*h}");
   }

   public static void LogCurveHeight(float[,] map, AnimationCurve curve, float multiplier,string label)
   {
      int w = map.GetLength(0), h = map.GetLength(1);
      float min = float.MaxValue, max = float.MinValue;
      int nan = 0;
      for (int y = 0; y < h; y++)
      for (int x = 0; x < w; x++)
      {
         float v = curve.Evaluate(map[x,y]) * multiplier;
         if (float.IsNaN(v) || float.IsInfinity(v)) { nan++; continue; }
         min = Mathf.Min(min, v);
         max = Mathf.Max(max, v);
      }
      Debug.Log($"{label} → min={min:F3}, max={max:F3}, NaNs={nan}/{w*h}");
   }

   public static void LogVerticesStats(Vector3[] vertices, string label)
   {
      float min = float.MaxValue, max = float.MinValue;
      
      int nan = 0;

      for (int i = 0; i < vertices.Length; i++)
      {
         float v = vertices[i].y;
         if (float.IsNaN(v) || float.IsInfinity(v)) { nan++; continue; }
         min = Mathf.Min(min, v);
         max = Mathf.Max(max, v);
      }
      Debug.Log($"{label} -> min = {min:F3}, max = {max:F3}, NaNs={nan}/{vertices.Length}");
      
   }
}
