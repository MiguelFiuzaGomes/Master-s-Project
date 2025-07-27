using UnityEngine;

public static class TextureGenerator
{
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        Texture2D texture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        // colourMap = BoxBlur(colourMap, width, height, 1);
        // colourMap = GaussianBlur(colourMap, width, height, 1);

        texture.SetPixels(colourMap);
        texture.Apply();

        return texture;
    }

    public static Texture2D TextureFromHeightMap(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[x + y * width] = Color.Lerp(Color.black, Color.white, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D VoronoiTexture(int gridSize, Color[] zoneColors, int width, int height)
    {
        //Set up
        Texture2D texture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point
        };
        int pixelPerCell = width / gridSize;

        // Generate Points
        Vector2Int[,] pointPositions = new Vector2Int[width, height];
        GenerateVoronoiSites(pointPositions, pixelPerCell, gridSize);


        // Return full Voronoi
        return texture;
    }

    static void GenerateVoronoiSites(Vector2Int[,] pointPositions, int ppC, int gridSize)
    {
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < gridSize; j++)
            {
                pointPositions[i, j] = new Vector2Int(i * ppC + Random.Range(0, ppC), j * ppC + Random.Range(0, ppC));
            }
        }
    }

    public static Texture2D TextureFromTemperature(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[x + y * width] = Color.Lerp(Color.blue, Color.red, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromHumidity(float[,] heightMap)
    {
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                colourMap[x + y * width] = Color.Lerp(Color.cyan, Color.blue, heightMap[x, y]);
            }
        }

        return TextureFromColourMap(colourMap, width, height);
    }

    public static Texture2D TextureFromVoronoi(float[,] heightMap, int chunkSize, Vector2Int[,] closestSite)
    {
        Color[] colourMap = new Color[chunkSize * chunkSize];

        // Determine grid dimensions
        int maxGridX = 0;
        int maxGridY = 0;

        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                Vector2Int point = closestSite[x, y];
                if (point.x > maxGridX) maxGridX = point.x;
                if (point.y > maxGridY) maxGridY = point.y;
            }
        }

        int gridSizeX = maxGridX + 1;
        int gridSizeY = maxGridY + 1;

        // Initialize to 0
        float[,] regionMax = new float[gridSizeX, gridSizeY];
        for(int gx = 0; gx < gridSizeX; gx ++)        
            for (int gy = 0; gy < gridSizeY; gy++)
                regionMax[gx, gy] = 0f;

        // scan all pixels to update distance
        for (int y = 0; y < chunkSize; y++)
        {
            for (int x = 0; x < chunkSize; x++)
            {
                Vector2Int site = closestSite[x, y];
                float d = heightMap[x, y];
                if(d > regionMax[site.x, site.y])
                    regionMax[site.x, site.y] = d;
            }
        }

        // Build the colour map
        for (int x = 0; x < chunkSize; x++)
        {
            for (int y = 0; y < chunkSize; y++)
            {
                Vector2Int site = closestSite[x, y];
                float d = heightMap[x, y];
                float maxD = regionMax[site.x, site.y];

                float t = maxD <= 0f ? 0f : // if the cell collapsed to the center, make it black
                    Mathf.Clamp01(d / maxD); 
                
                colourMap[y * chunkSize + x] = Color.Lerp(Color.black, Color.white, t);
            }
        }
        
        return TextureFromColourMap(colourMap, chunkSize, chunkSize);
    }

    private static Color[] BoxBlur(Color[] colors, int width, int height, int radius)
    {
        Color[] blur = new Color[colors.Length];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color sum = Color.black;
                int count = 0;
                
                // sample in a 2*radius+1 by 2*radius +1 area
                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int ny = y + dy;
                        int nx = x + dx;

                        // avoid out of bounds
                        if (nx < 0 || nx >= width || ny < 0 || ny >= height)
                            continue;
                        
                        sum += colors[ny * width + nx];
                        count++;
                    }
                }
                blur[y * width + x] = sum / count;
            }
        }
        
        return blur;
    }

    
    private static Color[] GaussianBlur(Color[] src, int width, int height, float sigma)
    {
        // build 1D kernel
        int radius = Mathf.CeilToInt(3 * sigma);
        float[] kernel = new float[2 * radius + 1];
        float twoSigma2 = 2 * sigma * sigma;
        float sum = 0;
        for (int i = -radius; i <= radius; i++)
        {
            float v = Mathf.Exp(-(i*i) / twoSigma2);
            kernel[i + radius] = v;
            sum += v;
        }
        // normalize
        for (int i = 0; i < kernel.Length; i++)
            kernel[i] /= sum;

        var tmp = new Color[src.Length];
        var dst = new Color[src.Length];

        // horizontal pass
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color col = Color.black;
                for (int k = -radius; k <= radius; k++)
                {
                    int xx = Mathf.Clamp(x + k, 0, width - 1);
                    col += src[y * width + xx] * kernel[k + radius];
                }
                tmp[y * width + x] = col;
            }
        }

        // vertical pass
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color col = Color.black;
                for (int k = -radius; k <= radius; k++)
                {
                    int yy = Mathf.Clamp(y + k, 0, height - 1);
                    col += tmp[yy * width + x] * kernel[k + radius];
                }
                dst[y * width + x] = col;
            }
        }

        return dst;
    }
}
