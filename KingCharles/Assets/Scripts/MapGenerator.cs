using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeabunkMapGenerator : MonoBehaviour
{
    [Header("Harita Ayarlarý")]
    public Vector2Int mapSize = new Vector2Int(20, 20);
    [Range(0, 1)] public float hilliness = 0.15f;
    public float blockSize = 2f;

    [Header("Prefablar")]
    public GameObject blockPrefab;
    public GameObject slopePrefab;

    [Header("Rampa Ayarý (Kritik)")]
    [Tooltip("Rampanýn yönü yanlýþsa burayý 90, 180, -90 deðiþtirerek Sarý Ok ile hizala.")]
    public float slopeRotationOffset = 0f;

    // Debug için listeyi public yapýp Inspector'da görmeni saðlayabiliriz ama
    // Gizmos çizimi için özel bir liste tutacaðýz.
    private List<GameObject> debugSlopeObjects = new List<GameObject>();

    private class BlockInfo
    {
        public Vector2Int coordinates;
        public int floorHeight;
        public int topHeight;
        public GameObject obj;

        public BlockInfo(Vector2Int coord, int floorH, int topH, GameObject o)
        {
            coordinates = coord;
            floorHeight = floorH;
            topHeight = topH;
            obj = o;
        }
    }

    private List<BlockInfo> spawnedBlocks = new List<BlockInfo>();
    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    void Start()
    {
        GenerateMap();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ClearMap();
            GenerateMap();
        }
    }

    void GenerateMap()
    {
        int totalBlocks = mapSize.x * mapSize.y;

        Vector2Int startPos = new Vector2Int(Random.Range(0, mapSize.x), Random.Range(0, mapSize.y));
        CreateBlock(startPos, 0, false, Vector2Int.zero);

        BlockInfo currentBlock = spawnedBlocks[0];

        while (spawnedBlocks.Count < totalBlocks)
        {
            List<Vector2Int> neighbors = GetFreeNeighbors(currentBlock.coordinates);

            if (neighbors.Count > 0)
            {
                Vector2Int nextPos = neighbors[Random.Range(0, neighbors.Count)];
                Vector2Int direction = nextPos - currentBlock.coordinates; // Hareket yönümüz (Örn: (0,1) Kuzey)

                int spawnHeight = currentBlock.topHeight;

                bool wantToRaise = Random.value < (hilliness / 2f);
                bool isSlope = false;

                if (wantToRaise)
                {
                    // Rampanýn çýkacaðý yerde boþluk var mý kontrolü
                    if (HasSpaceForLanding(nextPos))
                    {
                        isSlope = true;
                    }
                }

                CreateBlock(nextPos, spawnHeight, isSlope, direction);
                currentBlock = spawnedBlocks[spawnedBlocks.Count - 1];
            }
            else
            {
                // Backtracking
                bool foundNewPath = false;
                for (int i = 0; i < spawnedBlocks.Count; i++)
                {
                    if (GetFreeNeighbors(spawnedBlocks[i].coordinates).Count > 0)
                    {
                        currentBlock = spawnedBlocks[i];
                        foundNewPath = true;
                        break;
                    }
                }
                if (!foundNewPath) break;
            }
        }
    }

    void CreateBlock(Vector2Int pos, int height, bool isSlope, Vector2Int comingFromDir)
    {
        Vector3 worldPos = new Vector3(pos.x * blockSize, height * blockSize, pos.y * blockSize);
        Quaternion rotation = Quaternion.identity;
        int resultingTopHeight = height;

        if (isSlope)
        {
            resultingTopHeight = height + 1;

            // --- YENÝ VE KESÝN ROTASYON HESABI ---
            // Yön vektörünü 3D dünyaya çeviriyoruz (Y ekseni 0)
            Vector3 lookDir = new Vector3(comingFromDir.x, 0, comingFromDir.y);

            // Eðer yön sýfýrsa (baþlangýç bloðu gibi) iþlem yapma
            if (lookDir != Vector3.zero)
            {
                // Unity'nin LookRotation fonksiyonu, Z eksenini (Mavi ok) bu yöne çevirir.
                // Senin rampanýn "Yüksek" tarafý modelin Z eksenindeyse offset 0 olmalý.
                Quaternion baseRot = Quaternion.LookRotation(lookDir);
                rotation = baseRot * Quaternion.Euler(0, slopeRotationOffset, 0);
            }
        }

        GameObject prefabToUse = isSlope ? slopePrefab : blockPrefab;
        GameObject mainObj = Instantiate(prefabToUse, worldPos, rotation);
        mainObj.transform.localScale = Vector3.one * blockSize;
        mainObj.transform.parent = this.transform;
        mainObj.name = isSlope ? $"Slope_{pos}" : $"Block_{pos}";

        // Debug listesine ekle (Gizmos için)
        if (isSlope) debugSlopeObjects.Add(mainObj);

        // Altýný doldur
        for (int h = height - 1; h >= 0; h--)
        {
            Vector3 fillerPos = new Vector3(pos.x * blockSize, h * blockSize, pos.y * blockSize);
            GameObject filler = Instantiate(blockPrefab, fillerPos, Quaternion.identity);
            filler.transform.localScale = Vector3.one * blockSize;
            filler.transform.parent = this.transform;
        }

        BlockInfo info = new BlockInfo(pos, height, resultingTopHeight, mainObj);
        spawnedBlocks.Add(info);
        occupiedPositions.Add(pos);
    }

    bool HasSpaceForLanding(Vector2Int pos)
    {
        int freeCount = 0;
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int checkPos = pos + dir;
            if (IsValidAndFree(checkPos)) freeCount++;
        }
        return freeCount >= 1;
    }

    List<Vector2Int> GetFreeNeighbors(Vector2Int pos)
    {
        List<Vector2Int> freeNeighbors = new List<Vector2Int>();
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        foreach (var dir in dirs)
        {
            Vector2Int checkPos = pos + dir;
            if (IsValidAndFree(checkPos))
            {
                freeNeighbors.Add(checkPos);
            }
        }
        return freeNeighbors;
    }

    bool IsValidAndFree(Vector2Int pos)
    {
        return (pos.x >= 0 && pos.x < mapSize.x &&
                pos.y >= 0 && pos.y < mapSize.y &&
                !occupiedPositions.Contains(pos));
    }

    void ClearMap()
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        spawnedBlocks.Clear();
        occupiedPositions.Clear();
        debugSlopeObjects.Clear();
    }

    // --- GÖRSEL HATA AYIKLAMA (GIZMOS) ---
    // Scene penceresinde rampalarýn üzerinde sarý oklar çizer.
    // Bu oklar kodun "Rampa bu yöne bakmalý" dediði yöndür.
    // Eðer rampa baþka yere bakýyorsa SlopeRotationOffset ayarýný deðiþtirmen gerekir.
    void OnDrawGizmos()
    {
        if (debugSlopeObjects == null) return;

        Gizmos.color = Color.yellow;
        foreach (var obj in debugSlopeObjects)
        {
            if (obj != null)
            {
                // Objenin merkezinden ileri doðru bir ok çiz
                Vector3 start = obj.transform.position + Vector3.up * blockSize; // Biraz yukarýdan çiz
                Vector3 direction = obj.transform.forward * blockSize;

                // Oku çiz
                Gizmos.DrawRay(start, direction);
                // Okun ucuna küçük bir küre koy ki yönü belli olsun
                Gizmos.DrawSphere(start + direction, blockSize * 0.1f);
            }
        }
    }
}