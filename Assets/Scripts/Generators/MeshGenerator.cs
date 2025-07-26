using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
    // Generate the MeshData used to create the mesh (vertices, uvs and triangles)
    public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _heightCurve, int levelOfDetail)
    {
        // Hold an animation curve per thread since this would break otherwise
        AnimationCurve heightCurve = new AnimationCurve(_heightCurve.keys);
        
        // Clamp height curve
        heightCurve.preWrapMode = WrapMode.Clamp;
        heightCurve.postWrapMode = WrapMode.Clamp;
        
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        // LOD
        int simplificationIncrement = (levelOfDetail==0) ? 1 : levelOfDetail * 2;
        int verticesPerLine = (width - 1) / simplificationIncrement + 1;

        // Used to center the mesh
        float topLeftX = (width-1) / -2f;
        float topLeftZ = (height-1) / 2f;
        
        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;
        
        
        // Create the vertices, uvs and triangles for the mesh
        for (int y = 0; y < height; y+= simplificationIncrement)
        {
            for (int x = 0; x < width; x+= simplificationIncrement)
            {
                meshData.vertices[vertexIndex] = new Vector3(topLeftX + x, heightCurve.Evaluate(heightMap[x,y]) * heightMultiplier, topLeftZ - y);
                
                meshData.uvs[vertexIndex] = new Vector2(x / (float)width, y / (float)height);
                
                if (x < width - 1 && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine + 1, vertexIndex, vertexIndex + 1);
                }
                
                vertexIndex++;
            }
        }
        
        return meshData;
    }
}

public class MeshData
{
    public Vector3[] vertices;
    public int[] triangles;
    public Vector2[] uvs;

    private int triangleIndex; 
    
    // Constructor
    public MeshData(int meshWidth, int meshHeight)
    {
        vertices = new Vector3[meshWidth * meshHeight];
        uvs = new Vector2[meshWidth * meshHeight];
        triangles = new int[(meshWidth-1) * (meshHeight-1) * 6];
    }

    // Create triangles
    public void AddTriangle(int a, int b, int c)
    {
        triangles[triangleIndex] = a;
        triangles[triangleIndex + 1] = b;
        triangles[triangleIndex + 2] = c;
        
        triangleIndex += 3;
    }

    // Create a mesh after setting the vertices, uvs and triangles
    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        
        mesh.RecalculateNormals();

        return mesh;
    }
}