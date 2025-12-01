using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Referanslar")]
    public Transform player;          // Oyuncu transform'u
    public GameObject[] enemyPrefabs; // Inspector'dan atacaðýn düþman prefablarý

    [Header("Spawn Ayarlarý")]
    public float innerRadius = 5f;    // Bu çemberin içinde spawn OLMAYACAK
    public float outerRadius = 15f;   // Bu çemberin içinde (inner–outer arasý) spawn OLACAK
    public float spawnInterval = 0.5f;  // Kaç saniyede bir spawn
    public int maxEnemies = 1000;       // Ayný anda sahnede olabilecek max düþman

    private float timer;
    private List<GameObject> spawnedEnemies = new List<GameObject>();

    private void Update()
    {
        if (player == null || enemyPrefabs == null || enemyPrefabs.Length == 0)
            return;

        // Spawner'ý sürekli oyuncunun pozisyonuna taþýyoruz
        // (istersen bunu kaldýrýp spawner'ý sabit bir yere de koyabilirsin)
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

        // 3D dünyada XZ düzlemine projeksiyon
        Vector3 offset = new Vector3(randomDir.x, 0f, randomDir.y) * radius;
        Vector3 spawnPos = player.position + offset;

        // Rastgele bir düþman prefabi seç
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = Instantiate(prefab, spawnPos, Quaternion.identity);
        spawnedEnemies.Add(enemy);
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
