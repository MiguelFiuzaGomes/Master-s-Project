using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;
    
    // Mesh Creation
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    [Header("Textured Material")] 
    public Material finalMaterial;

    public void DrawTexture(Texture2D texture, List<SO_Biome> biomes)
    {
        textureRenderer.sharedMaterial.SetTexture("_Texture2D", texture);
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
        
        PlaneTextureFromColour(texture);
    }

    public void DrawMesh(MeshData meshData, Texture2D texture)
    {
        meshRenderer.sharedMaterial.SetTexture("_Texture2D", texture);
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = texture;
        
        MeshTextureFromColour(texture);
    }

    
    // Switch the material to apply a new shader
    private void PlaneTextureFromColour(Texture2D texture)
    {
        textureRenderer.sharedMaterial = finalMaterial;
        
        print("Plane material: " + textureRenderer.sharedMaterial.name);
        textureRenderer.sharedMaterial.SetTexture("_MainTex", texture);
        textureRenderer.sharedMaterial.mainTexture = texture;
        
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
    private void MeshTextureFromColour(Texture2D texture)
    {
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Repeat;
        
        meshRenderer.sharedMaterial = finalMaterial;
        meshRenderer.sharedMaterial.SetTexture("_MainTex", texture);

        meshRenderer.sharedMaterial.mainTexture = texture;
        meshRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }
    
    
}
