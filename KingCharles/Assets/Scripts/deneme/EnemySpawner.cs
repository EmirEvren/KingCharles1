using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Aktiflik")]
    [Tooltip("Kart seçilene kadar FALSE kalacak. WeaponChoiceManager açacak.")]
    public bool canSpawn = false;

    [Header("Referanslar")]
    public Transform player;          // Oyuncu transform'u
    public GameObject[] enemyPrefabs; // Inspector'dan atacaðýn düþman prefablarý

    [Header("Spawn Ayarlarý")]
    public float innerRadius = 5f;      // Bu çemberin içinde spawn OLMAYACAK
    public float outerRadius = 15f;     // Bu çemberin içinde (inner–outer arasý) spawn OLACAK
    public float spawnInterval = 0.5f;  // Kaç saniyede bir spawn
    public int maxEnemies = 1000;       // Ayný anda sahnede olabilecek max düþman

    [Header("Zemin Raycast")]
    [Tooltip("Spawn noktasýnýn ne kadar üstünden aþaðý ray atýlacak")]
    public float groundRayHeight = 50f;
    [Tooltip("Ray'in maksimum mesafesi")]
    public float groundRayDistance = 100f;

    private float timer;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    // ELITE SAYACI: her 5. düþman elite
    private int totalSpawnedCount = 0;

    private void Update()
    {
        // Kart seçilmeden çalýþmasýn
        if (!canSpawn) return;

        if (player == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // Spawner'ý sürekli oyuncunun pozisyonuna taþýyoruz
        transform.position = player.position;

        // Ölü düþmanlarý listeden temizle
        spawnedEnemies.RemoveAll(e => e == null);

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

        // Yarýçapý innerRadius ile outerRadius arasýnda seç
        float radius = Random.Range(innerRadius, outerRadius);

        // 3D dünyada XZ düzlemine projeksiyon (player'ý referans al)
        Vector3 flatOffset = new Vector3(randomDir.x, 0f, randomDir.y) * radius;
        Vector3 basePos = player.position + flatOffset;

        // --- ZEMÝN RAYCAST ---
        Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;
        Vector3 finalSpawnPos = basePos;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance))
        {
            // Ne bulduysa (Terrain, MeshCollider, vb.) onun üstüne spawn ol
            finalSpawnPos = hit.point;
        }
        else
        {
            // Hiçbir þey bulamadýysak, güvenlik için spawn etme
            // (Ýstersen burayý yorum satýrýna alýp basePos'tan da spawn edebilirsin)
            return;
        }

        // Rastgele bir düþman prefabi seç
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = Instantiate(prefab, finalSpawnPos, Quaternion.identity);
        spawnedEnemies.Add(enemy);

        // ---- ELITE MANTIK: her 5. düþman elite ----
        totalSpawnedCount++;
        bool isElite = (totalSpawnedCount % 5 == 0);

        if (isElite && enemy != null)
        {
            // 1) Scale x2
            enemy.transform.localScale *= 2f;

            // 2) Can = 150
            EnemyHealth health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                // EnemyHealth içine daha önce eklediðimiz fonksiyon:
                // public void SetMaxHealth(float newMaxHealth, bool fullHeal = true)
                health.SetMaxHealth(150f, true);
            }
        }
    }

    // Sahne içinde çemberleri görmek için
    private void OnDrawGizmosSelected()
    {
        if (player == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(player.position, innerRadius); // Ýç çember (boþ bölge)

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(player.position, outerRadius); // Dýþ çember (spawn bölgesinin sýnýrý)
    }
}
