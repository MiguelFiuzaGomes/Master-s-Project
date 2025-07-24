using System.Collections.Generic;
using Structs;
using UnityEngine;

public static class NoiseMapGenerator
{
    public static float[,] GenerateNoiseMap(NoiseSettings settings, Vector2 center, NormalizeMode normalizeMode, int width, int height)
    {
        float[,] noiseMap = new float[width, height];
        
        switch (settings.noiseType)
        {
            case NoiseType.Perlin:
                noiseMap = Noise.GeneratePerlinNoiseMap(width, height, settings.seed, settings.scale, settings.offset, normalizeMode);
                break;
            case NoiseType.FBM:
                noiseMap = Noise.GenerateFBMNoiseMap(width, height , settings.seed, settings.scale, settings.octaves, settings.persistence, settings.lacunarity, settings.offset, normalizeMode);
                break;
            case NoiseType.Ridge:
                noiseMap = Noise.GenerateRidgeNoiseMap(width, height, settings.seed, settings.scale, settings.octaves, settings.persistence, settings.lacunarity, settings.offset, normalizeMode);
                break;
            case NoiseType.DomainWarping:
                noiseMap = Noise.GenerateDomainWarpedNoiseMap(width, height, settings.seed, settings.scale, settings.warpScale, settings.warpStrength, settings.octaves, settings.persistence, settings.lacunarity, settings.offset, normalizeMode);
                break;
            // case NoiseType.Voronoi:
            //     noiseMap = Noise.GenerateVoronoiNoiseMap(width, gridSize, heightSeed, ref closestSites, ref secondClosestSites, out sitePositions);
            //     break;
            default:
                noiseMap = Noise.GeneratePerlinNoiseMap(width, height, settings.seed, settings.scale, settings.offset, normalizeMode);
                break;
        }

        return noiseMap;
    }
}

