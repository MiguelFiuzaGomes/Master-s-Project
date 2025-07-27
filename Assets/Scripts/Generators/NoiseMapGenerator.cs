using System.Collections.Generic;
using Structs;
using UnityEngine;

public static class NoiseMapGenerator
{
    public static float[,] GenerateNoiseMap(NoiseSettings settings, Vector2 centre, NormalizeMode normalizeMode, int width, int height)
    {
        float[,] noiseMap = new float[width, height];
        
        switch (settings.noiseType)
        {
            case NoiseType.Perlin:
                noiseMap = Noise.GeneratePerlinNoiseMap(width, height, settings, centre + settings.offset, normalizeMode);
                break;
            case NoiseType.FBM:
                noiseMap = Noise.GenerateFBMNoiseMap(width, height , settings, centre + settings.offset, normalizeMode);
                break;
            case NoiseType.Ridge:
                noiseMap = Noise.GenerateDomainWarpedRidges(width, height, settings, centre + settings.offset, normalizeMode, settings.padding);
                break;
            case NoiseType.DomainWarping:
                noiseMap = Noise.GenerateDomainWarpedNoiseMap(width, height, settings, centre + settings.offset, normalizeMode, settings.padding);
                break;
            // case NoiseType.Voronoi:
            //     noiseMap = Noise.GenerateVoronoiNoiseMap(width, gridSize, heightSeed, ref closestSites, ref secondClosestSites, out sitePositions);
            //     break;
            default:
                noiseMap = Noise.GeneratePerlinNoiseMap(width, height, settings, centre + settings.offset, normalizeMode);
                break;
        }

        // Specific situation for domain warping to maintain standardized approach on the rest of the program
        if (settings.noiseType == NoiseType.DomainWarping)
        {
            float[,] trimmed = new float[width, height];
            
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    trimmed[x, y] = noiseMap[x + settings.padding, y + settings.padding];
            
            noiseMap = trimmed;
        }

        return noiseMap;
    }

    public static (float min, float max) EstimateNoiseRange(NoiseSettings settings, int sampleWidth = 256,
        int sampleHeight = 256 )
    {
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;
        
        float[,] map = GenerateNoiseMap(settings, Vector2.zero, NormalizeMode.None, sampleWidth, sampleHeight);

        foreach (float value in map)
        {
            minVal = Mathf.Min(minVal, value);
            maxVal = Mathf.Max(maxVal, value);
        }

        return (minVal, maxVal);
    }
}

