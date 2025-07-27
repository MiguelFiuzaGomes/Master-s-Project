using UnityEngine;

namespace Structs
{
    public struct MapData
    {
        public readonly float[,] heightMap;
        public readonly Color[] colourMap;
        public readonly float[,] temperatureMap;
        public readonly float[,] humidityMap;
        public readonly float[,] ridgesMap;

        // Original Code
        public MapData(float[,] heightMap, Color[] colourMap)
        {
            this.heightMap = heightMap;
            this.colourMap = colourMap;
            this.temperatureMap = new float[0, 0];
            this.temperatureMap[0,0] = 0;
            this.humidityMap = new float[0, 0];
            this.humidityMap[0,0] = 0;
            this.ridgesMap = new float[0, 0];
            this.ridgesMap[0,0] = 0;
        }
   
        public MapData(float[,] heightMap, Color[] colourMap, float[,] temperatureMap, float[,] humidityMap, float[,] ridgesMap)
        {
            this.heightMap = heightMap;
            this.colourMap = colourMap;
            this.temperatureMap = temperatureMap;
            this.humidityMap = humidityMap;
            this.ridgesMap = ridgesMap;
        }
    }
}

