using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [Header("Aktiflik")]
    [Tooltip("Kart seçilene kadar FALSE kalacak. WeaponChoiceManager açacak.")]
    public bool canSpawn = false;

    [Header("Referanslar")]
    public Transform player;          // Oyuncu transform'u
    public GameObject[] enemyPrefabs; // Inspector'dan atacağın düşman prefabları

    [Header("Spawn Ayarları")]
    public float innerRadius = 5f;      // Bu çemberin içinde spawn OLMAYACAK
    public float outerRadius = 15f;     // Bu çemberin içinde (inner–outer arası) spawn OLACAK
    public float spawnInterval = 0.5f;  // Kaç saniyede bir spawn
    public int maxEnemies = 1000;       // Aynı anda sahnede olabilecek max düşman

    [Header("Zemin Raycast")]
    [Tooltip("Spawn noktasının ne kadar üstünden aşağı ray atılacak")]
    public float groundRayHeight = 50f;
    [Tooltip("Ray'in maksimum mesafesi")]
    public float groundRayDistance = 100f;

    [Header("Ground Mask (Opsiyonel)")]
    [Tooltip("Sadece zemin layer'larını seçmek istersen kullan. Default: Everything")]
    public LayerMask groundMask = ~0;

    [Header("Ground Snap (Opsiyonel)")]
    [Tooltip("Spawn sonrası collider altını zemine oturtmak için ekstra offset")]
    public float groundSnapEpsilon = 0.02f;

    [Header("MiniBoss (Her 3 Dakika Dalga)")]
    public GameObject miniBossPrefab;
    public float miniBossInterval = 180f;   // 3 dakika
    public float miniBossMaxHealth = 500f;
    public float miniBossDamage = 25f;
    public float miniBossMoveSpeed = 2.5f;  // çok hızlı olmasın

    private float elapsedGameTime = 0f;

    // 0 = hiç dalga spawn olmadı
    private int lastMiniBossWave = 0;

    private float timer;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // ELITE SAYACI: her 5. düşman elite
    private int totalSpawnedCount = 0;

    private void Update()
    {
        // Kart seçilmeden çalışmasın
        if (!canSpawn) return;

        if (player == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // Spawner'ı sürekli oyuncunun pozisyonuna taşıyoruz
        transform.position = player.position;

        // Ölü düşmanları listeden temizle
        spawnedEnemies.RemoveAll(e => e == null);

        // Oyun süresi (canSpawn başladıktan sonra akar)
        elapsedGameTime += Time.deltaTime;

        // --- MİNİBOSS DALGA SİSTEMİ ---
        // 3:00'te wave=1, 6:00'te wave=2, 9:00'te wave=3 ...
        if (miniBossPrefab != null && miniBossInterval > 0f)
        {
            int currentWave = Mathf.FloorToInt(elapsedGameTime / miniBossInterval);

            // currentWave 0 iken spawn yok. 1 olunca 1 miniboss, 2 olunca 2 miniboss...
            if (currentWave > lastMiniBossWave)
            {
                // Eğer oyun bir anda zaman atladıysa (lag vs.), kaçırılan dalgaları da sırayla bas
                for (int wave = lastMiniBossWave + 1; wave <= currentWave; wave++)
                {
                    int toSpawn = wave; // dalga numarası kadar miniboss

                    for (int i = 0; i < toSpawn; i++)
                    {
                        if (spawnedEnemies.Count >= maxEnemies) break; // max sınırını aşmasın
                        SpawnMiniBoss();
                    }
                }

                lastMiniBossWave = currentWave;
            }
        }
        // -----------------------------

        timer += Time.deltaTime;

        if (timer >= spawnInterval && spawnedEnemies.Count < maxEnemies)
        {
            SpawnEnemy();
            timer = 0f;
        }
    }

    private void SpawnEnemy()
    {
        // Yönü rasgele seç (2D circle içinde bir vektör)
        Vector2 randomDir = Random.insideUnitCircle.normalized;

        // Yarıçapı innerRadius ile outerRadius arasında seç
        float radius = Random.Range(innerRadius, outerRadius);

        // 3D dünyada XZ düzlemine projeksiyon (player'ı referans al)
        Vector3 flatOffset = new Vector3(randomDir.x, 0f, randomDir.y) * radius;
        Vector3 basePos = player.position + flatOffset;

        // --- ZEMİN RAYCAST ---
        Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;
        Vector3 finalSpawnPos = basePos;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            finalSpawnPos = hit.point;
        }
        else
        {
            return;
        }

        // Rastgele bir düşman prefabi seç
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = Instantiate(prefab, finalSpawnPos, Quaternion.identity);
        spawnedEnemies.Add(enemy);

        // Spawn sonrası otomatik zemine indir / oturt
        SnapToGround(enemy);

        // ---- ELITE MANTIK: her 5. düşman elite ----
        totalSpawnedCount++;
        bool isElite = (totalSpawnedCount % 5 == 0);

        if (isElite && enemy != null)
        {
            enemy.transform.localScale *= 2f;

            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.SetMaxHealth(150f, true);
            }

            SnapToGround(enemy);
        }
    }

    private void SpawnMiniBoss()
    {
        // Yönü rasgele seç
        Vector2 randomDir = Random.insideUnitCircle.normalized;
        float radius = Random.Range(innerRadius, outerRadius);

        Vector3 flatOffset = new Vector3(randomDir.x, 0f, randomDir.y) * radius;
        Vector3 basePos = player.position + flatOffset;

        // Zemin raycast
        Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;

        if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        Vector3 spawnPos = hit.point;

        GameObject boss = Instantiate(miniBossPrefab, spawnPos, Quaternion.identity);
        spawnedEnemies.Add(boss);

        // Spawn sonrası zemine oturt
        SnapToGround(boss);

        // 500 HP + miniboss flag + drop totals
        EnemyHealth health = boss.GetComponent<EnemyHealth>();
        if (health != null)
        {
            health.SetMaxHealth(miniBossMaxHealth, true);
            health.isMiniBoss = true;
            health.miniBossTotalXP = 100;
            health.miniBossTotalGold = 100;
        }

        // Takip scriptine göre hız + hasar ayarı
        SimpleEnemyFollow follow = boss.GetComponent<SimpleEnemyFollow>();
        if (follow != null)
        {
            follow.damage = miniBossDamage;
            follow.moveSpeed = miniBossMoveSpeed;
        }

        NavMeshAgent agent = boss.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.speed = miniBossMoveSpeed;
        }
    }

    private void SnapToGround(GameObject enemy)
    {
        if (enemy == null) return;

        Vector3 origin = enemy.transform.position + Vector3.up * 10f;

        // RaycastAll ile atıp, "kendi collider'ına çarpma" durumunu filtreliyoruz.
        RaycastHit[] hits = Physics.RaycastAll(origin, Vector3.down, 200f, groundMask, QueryTriggerInteraction.Ignore);
        if (hits == null || hits.Length == 0) return;

        // En yakın "self olmayan" hit'i bul
        bool foundGround = false;
        float bestDist = float.PositiveInfinity;
        RaycastHit bestHit = default;

        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (h.collider == null) continue;

            // Kendi collider'ını (child dahil) yok say
            if (h.collider.transform.IsChildOf(enemy.transform)) continue;

            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit = h;
                foundGround = true;
            }
        }

        if (!foundGround) return;

        // Collider altını zemine oturt
        Collider[] cols = enemy.GetComponentsInChildren<Collider>();
        float minY = float.PositiveInfinity;
        bool found = false;

        foreach (var c in cols)
        {
            if (c == null) continue;
            if (c.isTrigger) continue;

            minY = Mathf.Min(minY, c.bounds.min.y);
            found = true;
        }

        Vector3 pos = enemy.transform.position;

        if (!found)
        {
            // Collider yoksa pivot'u zemine koy (fallback)
            pos.y = bestHit.point.y + groundSnapEpsilon;
            enemy.transform.position = pos;
        }
        else
        {
            float delta = (bestHit.point.y - minY) + groundSnapEpsilon;
            enemy.transform.position += new Vector3(0f, delta, 0f);
        }

        // NavMeshAgent varsa yeni pozisyona "warp" et (havada yürüme bugını azaltır)
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
        {
            agent.Warp(enemy.transform.position);
        }

        // Rigidbody varsa konumu syncle + düşey hızı sıfırla
        var rb = enemy.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            Physics.SyncTransforms();
        }
    }


    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, innerRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, outerRadius);
    }
}
