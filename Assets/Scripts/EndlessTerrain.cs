using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    // Viewer
    public static float maxViewDistance;
    public Transform viewer;
    public static Vector2 viewerPosition;
    private Vector2 oldViewerPosition;
    private const float viewerPositionUpdateThreshold = 125f;
    private const float squareViewerUpdateThreshold = viewerPositionUpdateThreshold * viewerPositionUpdateThreshold;

    [Header("Scale")]
    public float chunkScale = 5f;

    public static float scale = 1f;
    
    // LODs
    [Header("LODs")]
    public LODInfo[] detailLevels;
    
    // Chunk info
    private int chunkSize;
    private int chunksInViewDistance;
    
    // Generated terrain chunk holder
    public Transform terrainChunkHolder;
    
    // Material for the created mesh
    public Material material;
    
    // Hold info on generated chunks
    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>(); // coords, chunk
    
    // List to hold the terrain chunks visible last update, so we can deactivate them
    static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    // Reference for the Map Generator
    static MapGenerator mapGenerator;
    
    
    private void Start()
    {
        mapGenerator = FindFirstObjectByType <MapGenerator>();
        
        scale = chunkScale;
        
        maxViewDistance = detailLevels[detailLevels.Length - 1].visibleDistanceThreshold;
        
        chunkSize = MapGenerator.mapChunkSize - 1;
        chunksInViewDistance = Mathf.RoundToInt(maxViewDistance / chunkSize);
        
        // First Update so meshes get drawn
        UpdateVisibleChunks();
    }

    public void OnValidate()
    {
        if (scale <= 0)
            scale = 1f;
    }

    private void Update()
    {
        viewerPosition = new Vector2(viewer.position.x, viewer.position.z) / scale;

        if ((viewerPosition - oldViewerPosition).sqrMagnitude > squareViewerUpdateThreshold)
        {
            oldViewerPosition = viewerPosition;
            UpdateVisibleChunks();
        }
        
    }

    void UpdateVisibleChunks()
    {
        // Set all visible chunks last frame to invisble and clearing the list
        foreach (TerrainChunk terrainChunk in terrainChunksVisibleLastUpdate)
        {
            terrainChunk.SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();
        
        // Get the viewers current chunk coordinate
        int currentChunkCoordX = Mathf.RoundToInt(viewerPosition.x / chunkSize);
        int currentChunkCoordy = Mathf.RoundToInt(viewerPosition.y / chunkSize);

        // Loop through the visible chunks
        for (int yOffset = -chunksInViewDistance; yOffset < chunksInViewDistance; yOffset++)
        {
            for (int xOffset = -chunksInViewDistance; xOffset < chunksInViewDistance; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordy + yOffset);

                // update the visible terrain
                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, terrainChunkHolder, material));
                }
            }
        }
    }

    public class TerrainChunk
    {
        // Instantiate Mesh
        GameObject meshObject;
        MeshRenderer meshRenderer;
        MeshFilter meshFilter;
        
        // Position
        private Vector2 position;
        private Vector3 positionV3; // hold the position in 3D space
        
        // Bounds for distance calculation
        Bounds bounds;
        
        // LODs
        LODInfo[] detailLevels;
        private LODMesh[] lodMeshes;
        private int previousLODIndex = -1;
        
        // MapData
        private MapData mapData;
        private bool mapDataReceived;
        
        public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
        {
            this.detailLevels = detailLevels;
            
            
            position = coord*size;
            bounds = new Bounds(position, Vector2.one * size);
            
            positionV3 = new Vector3(position.x, 0, position.y);
            
            // Generate and setup new meshes
            meshObject = new GameObject("TerrainChunk");
            meshRenderer = meshObject.AddComponent<MeshRenderer>();
            meshRenderer.material = material;
            meshFilter = meshObject.AddComponent<MeshFilter>();
            
            meshObject.transform.position = positionV3 * scale;
            meshObject.transform.parent = parent;
            meshObject.transform.localScale = Vector3.one * scale;
            
            

            // Create the LOD meshes
            lodMeshes = new LODMesh[detailLevels.Length];
            for (int i = 0; i < detailLevels.Length; i++)
            {
                lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
            }
            
            SetVisible(false);

            // Get map data
            mapGenerator.RequestMapData(position, OnMapDataReceived);
        }
#region Threading

        void OnMapDataReceived(MapData mapData)
        {
            this.mapData = mapData;
            mapDataReceived = true;
            
            //Draw temperature map
            //Texture2D texture = TextureGenerator.TextureFromTemperatureMap(mapData.temperatureMap);
            Texture2D texture = TextureGenerator.TextureFromColourMap(mapData.colourMap, MapGenerator.mapChunkSize, MapGenerator.mapChunkSize);
            meshRenderer.material.SetTexture("_Texture2D", texture);
            meshRenderer.material.mainTexture = texture;
            
            UpdateTerrainChunk();
        }

#endregion

        public void UpdateTerrainChunk()
        {
            if (mapDataReceived)
            {
                float viewerDistFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(viewerPosition));
                bool visible = viewerDistFromNearestEdge <= maxViewDistance;

                if (visible)
                {
                    int lodIndex = 0;
                    for (int i = 0;
                         i < lodMeshes.Length - 1;
                         i++) // loop through the lods except for the last one, since visible would be false
                    {
                        if (viewerDistFromNearestEdge > detailLevels[i].visibleDistanceThreshold)
                        {
                            lodIndex = i + 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    if (lodIndex != previousLODIndex)
                    {
                        LODMesh lodMesh = lodMeshes[lodIndex]; // set the lod for the current mesh

                        if (lodMesh.hasMesh)
                        {
                            previousLODIndex = lodIndex;
                            meshFilter.mesh = lodMesh.mesh;
                        }
                        else if (!lodMesh.hasRequestedMesh)
                        {
                            lodMesh.RequestMesh(mapData);
                        }
                    }
                    
                    terrainChunksVisibleLastUpdate.Add(this);
                }

                SetVisible(visible);
            }
        }

        public void SetVisible(bool visible)
        {
            meshObject.SetActive(visible);
        }

        public bool IsVisible()
        {
            return meshObject.activeSelf;
        }
    }

#region LOD

    class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;

        private int lod;
        
        System.Action updateCallback;

        public LODMesh(int lod, System.Action updateCallback)
        {
            this.lod = lod;
            this.updateCallback = updateCallback;
        }

        void OnMeshDataReceived(MeshData meshData)
        {
            mesh = meshData.CreateMesh();
            hasMesh = true;
            
            updateCallback();
        }
        
        public void RequestMesh(MapData mapData)
        {
            hasRequestedMesh = true;
            mapGenerator.RequestMeshData(mapData, lod, OnMeshDataReceived);
        }
    }
    // When to change resolution
    [System.Serializable]
    public struct LODInfo
    {
        public int lod;
        public float visibleDistanceThreshold;
    }
    
#endregion
}
