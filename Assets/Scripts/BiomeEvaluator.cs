using System.Collections.Generic;
using UnityEngine;

// Biome evaluation
public class BiomeEvaluator
{
    private readonly List<SO_Biome> biomes;

    public BiomeEvaluator(List<SO_Biome> biomes)
    {
        this.biomes = biomes;
    }
    
    public SO_Biome Evaluate(float height, float temperature, float humidity)
    {
        foreach(SO_Biome biome in biomes)
            if (biome.MatchesConditions(height, temperature, humidity))
                return biome;
        return null;
    }

    public SO_Biome EvaluateBestMatch(float height, float temperature, float humidity)
    {
        SO_Biome bestBiome = null;
        float bestScore = float.MaxValue;

        foreach (SO_Biome biome in biomes)
        {
            float heightScore = Mathf.Abs(height - biome.minimumHeight);
            float temperatureScore = Mathf.Abs(temperature - biome.minimumTemperature);
            float humidityScore = Mathf.Abs(humidity - biome.minimumHumidity);
            
            float totalScore = heightScore + temperatureScore + humidityScore;
            
            float weightedScore = totalScore / (1 + biome.weightBias);
            
            if (weightedScore < bestScore)
            {
                bestScore = totalScore;
                bestBiome = biome;
            }
        }
        
        return bestBiome;
    }
}
