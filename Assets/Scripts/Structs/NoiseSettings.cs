using UnityEngine;

[System.Serializable]
public struct NoiseSettings
{
    [Header ("Noise Settings")]
    public NoiseType noiseType;
    public int seed;
    public float scale;
    [Range(1, 10)] public int octaves;
    [Range(0f, 1f)] public float persistence;
    [Range(1f, 5f)] public float lacunarity;
    public Vector2 offset;

    [Header("Domain Warping")]
    public int warpScale;
    public float warpStrength;

    [HideInInspector]
    public int padding;

    [HideInInspector] public float minVal;
    [HideInInspector] public float maxVal;

}
        