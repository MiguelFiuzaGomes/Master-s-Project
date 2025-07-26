using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/SO_Biome")]
public class SO_Biome : ScriptableObject
{
    public string name;
    
    [Header("Comparison Values")]
    [Range(0, 1)] public float minimumHeight;
    [Range(0,1 )] public float maximumHeight;
    [Range(0, 1)] public float minimumTemperature;
    [Range(0, 1)] public float maximumTemperature;
    [Range(0, 1)] public float minimumHumidity;
    [Range(0, 1)] public float maximumHumidity;
    
    [Header("Weight Bias")]
    [Range(0,1)] public float weightBias = 0f;
    
    [Header("Colour")]
    public Color colour;
    
    public bool MatchesConditions(float height, float temperature, float humidity)
    {
        bool bHeightCondition = height >= minimumHeight && height <= maximumHeight;
        bool bTemperatureCondition = temperature >= minimumTemperature && temperature <= maximumTemperature;
        bool bHumidityCondition = humidity >= minimumHumidity && humidity <= maximumHumidity;
        
        return bHeightCondition && bTemperatureCondition && bHumidityCondition;
    }

    public float EvaluateFitness(float height, float temperature, float humidity, AnimationCurve heightCurve,
        AnimationCurve temperatureCurve, AnimationCurve humidityCurve)
    {
        float hScore = heightCurve.Evaluate(height);
        float tScore = temperatureCurve.Evaluate(temperature);
        float mScore = humidityCurve.Evaluate(humidity);
        
        float totalScore = hScore * tScore * mScore;
        totalScore *= (1 + weightBias);
        
        return totalScore;
    }


    /*
     * TODO: Add assets
     *  Add ranges to the values (temp, hum)
     */

}
