using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    
    // Mesh Creation
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.SetTexture("_Texture2D", texture);
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture, Texture2D heightMap)
    {
        meshRenderer.sharedMaterial.SetTexture("_Texture2D", texture);
        meshRenderer.sharedMaterial.SetTexture("_HeightMap", heightMap);
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
    }
}
