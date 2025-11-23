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

    private int[,] elevationGrid;
    private bool[,] topIsSlope;
    private Quaternion[,] topRotation;

    // --- YENÝ: slope'un "yukarý baktýðý yönü" tutuyoruz ---
    private Vector2Int[,] slopeDir;

    private List<Vector2Int> spawnedBlocks;
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

        GenerateMapDataOnly();
        BuildMapFromData();
        SpawnVegetation();
    }

    // -------------------- 1) DATA ONLY GENERATION --------------------

    void GenerateMapDataOnly()
    {
        Transform old = transform.Find("GeneratedMap");
        if (old != null) Destroy(old.gameObject);

        elevationGrid = new int[mapWidth, mapDepth];
        topIsSlope = new bool[mapWidth, mapDepth];
        topRotation = new Quaternion[mapWidth, mapDepth];
        slopeDir = new Vector2Int[mapWidth, mapDepth]; // default (0,0)

        for (int x = 0; x < mapWidth; x++)
            for (int z = 0; z < mapDepth; z++)
                elevationGrid[x, z] = -1;

        int maxBlocks = mapWidth * mapDepth;
        spawnedBlocks = new List<Vector2Int>(maxBlocks);

        int startX = Random.Range(0, mapWidth);
        int startZ = Random.Range(0, mapDepth);

        elevationGrid[startX, startZ] = 0;
        topIsSlope[startX, startZ] = false;
        topRotation[startX, startZ] = Quaternion.identity;
        slopeDir[startX, startZ] = Vector2Int.zero;

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

        // --- YENÝ: Map bitti, iþe yaramayan rampalarý temizle ---
        ValidateSlopes();
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

    // slope target'ýn tepesinde oluþuyor (eski mantýk)
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

            Vector3 dir3 = new Vector3(target.x - src.x, 0, target.y - src.z);
            rot = Quaternion.LookRotation(dir3) * Quaternion.Euler(0, 180f, 0);

            // --- YENÝ: slope’un yukarý yönünü kaydet ---
            slopeDir[target.x, target.y] = new Vector2Int(target.x - src.x, target.y - src.z);
        }
        else
        {
            slopeDir[target.x, target.y] = Vector2Int.zero;
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

    // --- YENÝ: iþe yaramayan rampalarý normal bloða çevir ---
    private void ValidateSlopes()
    {
        for (int x = 0; x < mapWidth; x++)
        {
            for (int z = 0; z < mapDepth; z++)
            {
                if (!topIsSlope[x, z]) continue;

                int h = elevationGrid[x, z];
                Vector2Int dir = slopeDir[x, z];

                // yön yoksa direkt iptal
                if (dir == Vector2Int.zero)
                {
                    topIsSlope[x, z] = false;
                    topRotation[x, z] = Quaternion.identity;
                    continue;
                }

                int upX = x + dir.x;
                int upZ = z + dir.y;

                int downX = x - dir.x;
                int downZ = z - dir.y;

                bool hasDown = InBounds(downX, downZ) && elevationGrid[downX, downZ] == h - 1;
                bool hasUp = InBounds(upX, upZ) && elevationGrid[upX, upZ] >= h;

                if (!(hasDown && hasUp))
                {
                    // rampa boþa gidiyor -> normal bloða çevir
                    topIsSlope[x, z] = false;
                    topRotation[x, z] = Quaternion.identity;
                    slopeDir[x, z] = Vector2Int.zero;
                }
            }
        }
    }

    private bool InBounds(int x, int z)
    {
        return x >= 0 && x < mapWidth && z >= 0 && z < mapDepth;
    }

    // -------------------- 2) BUILD MAP FROM DATA (FAST) --------------------

    void BuildMapFromData()
    {
        int maxCells = mapWidth * mapDepth;
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

                // 1) Fill bloklar
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

                // slope altýnda blok yoksa ekle (önceki fix)
                if (topIsSlope[x, z])
                {
                    int supportY = topElev - 1;
                    if (supportY >= 0)
                    {
                        Vector3Int supportKey = new Vector3Int(x, supportY, z);

                        if (!allSpawnedBlocks.TryGetValue(supportKey, out GameObject supportObj))
                        {
                            Vector3 supportPos = new Vector3(x * blockScale.x, supportY * unitH, z * blockScale.z);
                            supportObj = Instantiate(normalBlockPrefab, supportPos, Quaternion.identity, mapParent);
                            supportObj.transform.localScale = blockScale;
                            supportObj.name = $"{normalBlockPrefab.name} (Support) ({x},{supportY},{z})";

                            if (supportObj.TryGetComponent(out Block sb))
                                sb.Init(false);

                            allSpawnedBlocks[supportKey] = supportObj;
                        }

                        if (supportObj.TryGetComponent(out MeshRenderer sr))
                            sr.enabled = true;
                    }
                }
            }
        }

        OptimizeMeshRenderers();
    }

    private void OptimizeMeshRenderers()
    {
        foreach (var entry in allSpawnedBlocks)
        {
            Vector3Int pos = entry.Key;
            GameObject obj = entry.Value;

            if (!obj.TryGetComponent(out MeshRenderer r))
                continue;

            // üstünde slope varsa gizleme (önceki fix)
            if (allSpawnedBlocks.TryGetValue(pos + Vector3Int.up, out GameObject upObj) &&
                upObj.TryGetComponent(out Block upBlock) && upBlock.IsSlope)
            {
                r.enabled = true;
                continue;
            }

            r.enabled = ShouldBeVisible(obj, pos);
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
