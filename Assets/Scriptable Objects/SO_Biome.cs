using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "ScriptableObjects/SO_Biome")]
public class SO_Biome : ScriptableObject
{
    [Header("Name")]
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
    
    [Header("Texture")]
    public Texture2D texture;
    
    /*
     * TODO: Add assets
     *  Add ranges to the values (temp, hum)
     */
        
}
