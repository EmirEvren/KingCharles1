using UnityEngine;
using System.Collections.Generic;

public class MapGenerator : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject normalBlockPrefab;
    public GameObject slopeBlockPrefab;

    [Header("Vegetation")]
    public GameObject treePrefab;
    public GameObject bushPrefab;

    [Header("Vegetation Settings")]
    public float vegetationSpawnChance = 1.0f;
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

    private int[,] elevationGrid;                 // top height per (x,z)
    private bool[,] topIsSlope;                   // top block type
    private Quaternion[,] topRotation;            // top block rotation

    private List<Vector2Int> spawnedBlocks;       // surface cells (same as before)
    private Dictionary<Vector3Int, GameObject> allSpawnedBlocks;

    private struct MapPoint
    {
        public int x, z, elevation;
        public MapPoint(int x, int z, int e) { this.x = x; this.z = z; this.elevation = e; }
    }

    void Start()
    {
        if (normalBlockPrefab == null || slopeBlockPrefab == null)
        {
            Debug.LogError("Lütfen normal ve slope prefablarýný ekleyin.");
            return;
        }

        GenerateMapDataOnly();   // 1) sadece veri üret
        BuildMapFromData();      // 2) tek passta instantiate
        SpawnVegetation();       // ayný
    }

    // -------------------- 1) DATA ONLY GENERATION --------------------

    void GenerateMapDataOnly()
    {
        Transform old = transform.Find("GeneratedMap");
        if (old != null) Destroy(old.gameObject);

        elevationGrid = new int[mapWidth, mapDepth];
        topIsSlope = new bool[mapWidth, mapDepth];
        topRotation = new Quaternion[mapWidth, mapDepth];

        for (int x = 0; x < mapWidth; x++)
            for (int z = 0; z < mapDepth; z++)
                elevationGrid[x, z] = -1; // boþ

        int maxBlocks = mapWidth * mapDepth;
        spawnedBlocks = new List<Vector2Int>(maxBlocks);

        int startX = Random.Range(0, mapWidth);
        int startZ = Random.Range(0, mapDepth);

        elevationGrid[startX, startZ] = 0;
        topIsSlope[startX, startZ] = false;
        topRotation[startX, startZ] = Quaternion.identity;
        spawnedBlocks.Add(new Vector2Int(startX, startZ));

        int attemptLimit = maxBlocks * 5;
        int spawnedCount = 1;

        while (spawnedCount < maxBlocks && attemptLimit > 0)
        {
            attemptLimit--;

            MapPoint? src = FindExpansionSource();
            if (!src.HasValue)
                break;

            if (TryExpandDataOnly(src.Value))
                spawnedCount++;
        }
    }

    private MapPoint? FindExpansionSource()
    {
        for (int i = 0; i < spawnedBlocks.Count; i++)
        {
            var p = spawnedBlocks[i];
            int h = elevationGrid[p.x, p.y];
            if (h < 0) continue;

            var nb = GetAvailableNeighbors(p.x, p.y);
            if (nb.Count > 0)
                return new MapPoint(p.x, p.y, h);
        }
        return null;
    }

    private bool TryExpandDataOnly(MapPoint src)
    {
        var nb = GetAvailableNeighbors(src.x, src.z);
        if (nb.Count == 0) return false;

        Vector2Int target = nb[Random.Range(0, nb.Count)];

        int newElev = src.elevation;
        bool makeSlope = false;
        Quaternion rot = Quaternion.identity;

        if (Random.value < hilliness / 2f)
        {
            newElev++;
            makeSlope = true;

            Vector3 dir = new Vector3(target.x - src.x, 0, target.y - src.z);
            rot = Quaternion.LookRotation(dir) * Quaternion.Euler(0, 180f, 0);
        }

        elevationGrid[target.x, target.y] = newElev;
        topIsSlope[target.x, target.y] = makeSlope;
        topRotation[target.x, target.y] = rot;

        spawnedBlocks.Add(target);
        return true;
    }

    private List<Vector2Int> GetAvailableNeighbors(int x, int z)
    {
        List<Vector2Int> list = new List<Vector2Int>(4);
        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int nz = z + dz[i];

            if (nx >= 0 && nx < mapWidth && nz >= 0 && nz < mapDepth)
            {
                if (elevationGrid[nx, nz] == -1)
                    list.Add(new Vector2Int(nx, nz));
            }
        }
        return list;
    }

    // -------------------- 2) BUILD MAP FROM DATA (FAST) --------------------

    void BuildMapFromData()
    {
        int maxCells = mapWidth * mapDepth;

        // yaklaþýk toplam blok sayýsý için kapasite büyük tut
        allSpawnedBlocks = new Dictionary<Vector3Int, GameObject>(maxCells * 4);

        Transform mapParent = new GameObject("GeneratedMap").transform;
        mapParent.SetParent(transform);

        float unitH = blockScale.y;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapDepth; z++)
            {
                int topElev = elevationGrid[x, z];
                if (topElev < 0) continue;

                // 1) Fill bloklar (0 .. topElev-1)
                for (int y = 0; y < topElev; y++)
                {
                    Vector3 pos = new Vector3(x * blockScale.x, y * unitH, z * blockScale.z);
                    var fill = Instantiate(normalBlockPrefab, pos, Quaternion.identity, mapParent);
                    fill.transform.localScale = blockScale;
                    fill.name = $"{normalBlockPrefab.name} (Fill) ({x},{y},{z})";

                    if (fill.TryGetComponent(out Block fb))
                        fb.Init(false);

                    allSpawnedBlocks[new Vector3Int(x, y, z)] = fill;
                }

                // 2) Top blok
                GameObject topPrefab = topIsSlope[x, z] ? slopeBlockPrefab : normalBlockPrefab;
                Quaternion topRot = topRotation[x, z];

                Vector3 topPos = new Vector3(x * blockScale.x, topElev * unitH, z * blockScale.z);
                var topObj = Instantiate(topPrefab, topPos, topRot, mapParent);
                topObj.transform.localScale = blockScale;
                topObj.name = $"{topPrefab.name} ({x},{z}) Elev {topElev}";

                if (topObj.TryGetComponent(out Block tb))
                    tb.Init(topIsSlope[x, z]);

                allSpawnedBlocks[new Vector3Int(x, topElev, z)] = topObj;
            }
        }

        OptimizeMeshRenderers();
    }

    private void OptimizeMeshRenderers()
    {
        foreach (var entry in allSpawnedBlocks)
        {
            var obj = entry.Value;
            if (!obj.TryGetComponent(out MeshRenderer r)) continue;

            r.enabled = ShouldBeVisible(obj, entry.Key);
        }
    }

    private bool ShouldBeVisible(GameObject blockObj, Vector3Int pos)
    {
        if (blockObj.TryGetComponent(out Block b) && b.IsSlope)
            return true;

        Vector3Int[] dirs = {
            Vector3Int.right, Vector3Int.left,
            Vector3Int.up, Vector3Int.down,
            Vector3Int.forward, Vector3Int.back
        };

        foreach (var d in dirs)
        {
            Vector3Int np = pos + d;

            if (!allSpawnedBlocks.TryGetValue(np, out GameObject nbObj))
                return true;

            if (nbObj.TryGetComponent(out Block nb) && nb.IsSlope)
                return true;
        }
        return false;
    }

    // ---------------- VEGETATION ----------------------

    private void SpawnVegetation()
    {
        if (treePrefab == null && bushPrefab == null)
            return;

        List<Vector3> used = new List<Vector3>(mapWidth * mapDepth);
        float minDistSqr = minVegetationDistance * minVegetationDistance;

        Transform mapParent = transform.Find("GeneratedMap");
        float unitH = blockScale.y;

        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapDepth; z++)
            {
                int topElev = elevationGrid[x, z];
                if (topElev < 0) continue;

                Vector3Int topKey = new Vector3Int(x, topElev, z);
                if (!allSpawnedBlocks.TryGetValue(topKey, out GameObject block))
                    continue;

                if (block.TryGetComponent(out Block b) && b.IsSlope)
                    continue;

                if (Random.value > vegetationSpawnChance)
                    continue;

                Vector3 top = new Vector3(x * blockScale.x, (topElev + 1) * unitH, z * blockScale.z);

                bool tooClose = false;
                for (int i = 0; i < used.Count; i++)
                {
                    Vector3 u = used[i];
                    float dx = u.x - top.x;
                    float dz = u.z - top.z;
                    if ((dx * dx + dz * dz) < minDistSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                GameObject chosen = (treePrefab != null && Random.value < 0.5f)
                    ? treePrefab
                    : bushPrefab;

                float randomYRotation = Random.Range(0f, 360f);
                GameObject veg = Instantiate(chosen, top, Quaternion.Euler(0, randomYRotation, 0), mapParent);

                float s = (chosen == treePrefab)
                    ? Random.Range(treeMinScale, treeMaxScale)
                    : Random.Range(bushMinScale, bushMaxScale);

                veg.transform.localScale = Vector3.one * s;

                used.Add(top);
            }
        }
    }
}
