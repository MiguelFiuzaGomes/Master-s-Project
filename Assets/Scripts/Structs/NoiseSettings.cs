using UnityEngine;

[System.Serializable]
public struct NoiseSettings
{
    public NoiseType noiseType;
    public int seed;
    public int scale;

    [Range(1, 10)] public int octaves;
    [Range(0f, 1f)] public float persistence;
    [Range(1f, 5f)] public float lacunarity;

    public int warpScale;
    public float warpStrength;

    public Vector2 offset;
}
        