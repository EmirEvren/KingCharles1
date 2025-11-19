using UnityEngine;
using System.Collections.Generic;
using static Unity.Collections.AllocatorManager;

public class MapGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject normalBlockPrefab;
    public GameObject slopeBlockPrefab;

    [Header("Vegetation")]
    public GameObject treePrefab;
    public GameObject bushPrefab;

    [Header("Vegetation Settings")]
    public float vegetationSpawnChance = 1.0f; // %100 test için
    public float minVegetationDistance = 0.5f;
    public float treeMinScale = 0.8f;
    public float treeMaxScale = 1.4f;
    public float bushMinScale = 0.6f;
    public float bushMaxScale = 1.1f;

    [Header("Map Dimensions")]
    public int mapWidth = 20;
    public int mapDepth = 20;

    [Header("Block Settings")]
    public Vector3 blockScale = Vector3.one;

    [Range(0f, 1f)]
    public float hilliness = 0.15f;

    private int[,] elevationGrid;
    private List<Vector2Int> spawnedBlocks;
    private Dictionary<Vector3Int, GameObject> allSpawnedBlocks;

    private struct MapPoint
    {
        public int x;
        public int z;
        public int elevation;
        public MapPoint(int x, int z, int e) { this.x = x; this.z = z; this.elevation = e; }
    }

    void Start()
    {
        if (normalBlockPrefab == null || slopeBlockPrefab == null)
        {
            Debug.LogError("Lütfen normal ve slope prefablarýný ekleyin.");
            return;
        }

        GenerateMap();
        SpawnVegetation();
    }

    void GenerateMap()
    {
        Transform old = transform.Find("GeneratedMap");
        if (old != null)
            DestroyImmediate(old.gameObject);

        elevationGrid = new int[mapWidth, mapDepth];
        spawnedBlocks = new List<Vector2Int>();
        allSpawnedBlocks = new Dictionary<Vector3Int, GameObject>();

        Transform mapParent = new GameObject("GeneratedMap").transform;
        mapParent.SetParent(transform);

        int startX = Random.Range(0, mapWidth);
        int startZ = Random.Range(0, mapDepth);

        MapPoint start = new MapPoint(startX, startZ, 0);
        SpawnBlock(start, normalBlockPrefab, mapParent, Quaternion.identity);

        elevationGrid[startX, startZ] = 0;
        spawnedBlocks.Add(new Vector2Int(startX, startZ));

        int maxBlocks = mapWidth * mapDepth;
        int attemptLimit = maxBlocks * 5;
        int spawnedCount = 1;

        while (spawnedCount < maxBlocks && attemptLimit > 0)
        {
            attemptLimit--;

            MapPoint? src = FindExpansionSource();
            if (!src.HasValue)
                break;

            if (TryExpand(src.Value, mapParent))
                spawnedCount++;
        }

        OptimizeMeshRenderers();
    }

    private MapPoint? FindExpansionSource()
    {
        foreach (var p in spawnedBlocks)
        {
            int h = elevationGrid[p.x, p.y];
            List<Vector2Int> nb = GetAvailableNeighbors(p.x, p.y);
            if (nb.Count > 0)
                return new MapPoint(p.x, p.y, h);
        }
        return null;
    }

    private bool TryExpand(MapPoint src, Transform parent)
    {
        List<Vector2Int> nb = GetAvailableNeighbors(src.x, src.z);
        if (nb.Count == 0) return false;

        Vector2Int target = nb[Random.Range(0, nb.Count)];

        int newElev = src.elevation;
        GameObject prefab = normalBlockPrefab;
        Quaternion rot = Quaternion.identity;

        if (Random.value < hilliness / 2f)
        {
            newElev++;
            prefab = slopeBlockPrefab;

            Vector3 dir = new Vector3(target.x - src.x, 0, target.y - src.z);
            rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180f, 0);
        }

        MapPoint np = new MapPoint(target.x, target.y, newElev);
        SpawnBlock(np, prefab, parent, rot);

        elevationGrid[target.x, target.y] = newElev;
        spawnedBlocks.Add(target);

        return true;
    }

    private void SpawnBlock(MapPoint p, GameObject prefab, Transform parent, Quaternion rot)
    {
        Vector3 worldPos = new Vector3(p.x * blockScale.x, 0, p.z * blockScale.z);

        GameObject obj = Instantiate(prefab, worldPos, rot, parent);
        obj.transform.localScale = blockScale;

        obj.name = $"{prefab.name} ({p.x},{p.z}) Elev {p.elevation}";

        Vector3Int key = new Vector3Int(p.x, p.elevation, p.z);
        allSpawnedBlocks[key] = obj;

        Block b = obj.GetComponent<Block>();
        if (b != null)
            b.Elevate(p.elevation, normalBlockPrefab, parent, allSpawnedBlocks, blockScale);
    }

    private List<Vector2Int> GetAvailableNeighbors(int x, int z)
    {
        List<Vector2Int> list = new List<Vector2Int>();
        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (nx >= 0 && nx < mapWidth && nz >= 0 && nz < mapDepth)
            {
                if (elevationGrid[nx, nz] == 0)
                    list.Add(new Vector2Int(nx, nz));
            }
        }
        return list;
    }

    private void OptimizeMeshRenderers()
    {
        foreach (var entry in allSpawnedBlocks)
        {
            GameObject obj = entry.Value;
            MeshRenderer r = obj.GetComponent<MeshRenderer>();
            if (r == null) continue;

            r.enabled = ShouldBeVisible(obj, entry.Key);
        }
    }

    private bool ShouldBeVisible(GameObject block, Vector3Int pos)
    {
        if (block.name.Contains(slopeBlockPrefab.name))
            return true;

        Vector3Int[] dirs = {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

        foreach (var d in dirs)
        {
            Vector3Int np = pos + d;

            if (!allSpawnedBlocks.ContainsKey(np))
                return true;

            GameObject nb = allSpawnedBlocks[np];
            if (nb.name.Contains(slopeBlockPrefab.name))
                return true;
        }

        return false;
    }

    // ---------------- VEGETATION (Sadece en üst bloklar) ----------------------

    private void SpawnVegetation()
    {
        if (treePrefab == null && bushPrefab == null)
            return;

        List<Vector3> used = new List<Vector3>();

        foreach (var entry in allSpawnedBlocks)
        {
            GameObject block = entry.Value;
            Vector3Int pos = entry.Key;

            // Rampa bloklarýna bitki gelmez
            if (block.name.Contains(slopeBlockPrefab.name))
                continue;

            // Üstünde blok varsa bitki gelmez
            Vector3Int abovePos = new Vector3Int(pos.x, pos.y + 1, pos.z);
            if (allSpawnedBlocks.ContainsKey(abovePos))
                continue;

            if (Random.value > vegetationSpawnChance)
                continue;

            // Bloðun tepe noktasý
            Vector3 top = block.transform.position + new Vector3(0, blockScale.y, 0);

            // Minimum mesafe kontrolü
            bool tooClose = false;
            foreach (var u in used)
            {
                if (Vector2.Distance(new Vector2(u.x, u.z), new Vector2(top.x, top.z)) < minVegetationDistance)
                {
                    tooClose = true;
                    break;
                }
            }
            if (tooClose) continue;

            // Prefab seç
            GameObject chosen = (treePrefab != null && Random.value < 0.5f) ? treePrefab : bushPrefab;

            // Rastgele Y rotasyonu ile spawn
            float randomYRotation = Random.Range(0f, 360f);
            GameObject veg = Instantiate(chosen, top, Quaternion.Euler(0, randomYRotation, 0), transform.Find("GeneratedMap"));

            // Random scale
            float s = 1f;
            if (chosen == treePrefab)
                s = Random.Range(treeMinScale, treeMaxScale);
            else
                s = Random.Range(bushMinScale, bushMaxScale);

            veg.transform.localScale = Vector3.one * s;

            used.Add(top);
        }
    }
}
