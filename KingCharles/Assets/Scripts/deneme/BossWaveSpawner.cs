using System.Collections;
using UnityEngine;

public class BossWaveSpawner : MonoBehaviour
{
    [Header("Boss Prefab")]
    public GameObject bossPrefab;

    [Header("Boss Fixed Stats (SABIT)")]
    public float fixedBossHealth = 20000f;
    public float fixedBossDamage = 200f;

    [Header("Schedule")]
    public int firstWaveMinute = 15;        // 15. dk
    public int waveIntervalMinutes = 5;     // sonra her 5 dk

    [Header("Spawn (Around Player)")]
    public string playerTag = "Animal";
    public float innerRadius = 14f;
    public float outerRadius = 22f;

    [Header("Ground Raycast")]
    public LayerMask groundMask = ~0;
    public float groundRayHeight = 50f;
    public float groundRayDistance = 200f;
    public int maxTriesPerBoss = 10;

    private Transform player;

    private int nextWaveMinute;
    private int nextWaveCount = 1;

    private void Awake()
    {
        nextWaveMinute = firstWaveMinute;
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null) player = p.transform;
        else Debug.LogWarning($"[BossWaveSpawner] Player tag '{playerTag}' bulunamadý.");
    }

    private void Update()
    {
        if (bossPrefab == null || player == null) return;

        // Öncelik: GameTimerUI varsa onu kullan, yoksa Time.timeSinceLevelLoad
        float t = (GameTimerUI.Instance != null) ? GameTimerUI.Instance.GetElapsedTime() : Time.timeSinceLevelLoad;
        int elapsedMinutes = Mathf.FloorToInt(t / 60f);

        // Kaçýrýlan dalga varsa hepsini sýrayla bas
        while (elapsedMinutes >= nextWaveMinute)
        {
            SpawnWave(nextWaveCount);

            nextWaveMinute += waveIntervalMinutes;
            nextWaveCount *= 2; // 1,2,4,8...
        }
    }

    private void SpawnWave(int count)
    {
        for (int i = 0; i < count; i++)
        {
            Vector3 pos;
            if (!TryGetGroundedSpawnPos(out pos))
            {
                Vector3 fallback = player.position + Random.onUnitSphere * outerRadius;
                fallback.y = player.position.y;
                pos = fallback;
            }

            GameObject boss = Instantiate(bossPrefab, pos, Quaternion.identity);

            // Kritik: EnemyHealth ve SimpleEnemyFollow kendi Start()'larýnda difficulty çarpýyor olabilir.
            // O yüzden 1 frame bekleyip SABÝT deðerleri yeniden set ediyoruz.
            StartCoroutine(ApplyFixedBossStatsNextFrame(boss));
        }
    }

    private IEnumerator ApplyFixedBossStatsNextFrame(GameObject boss)
    {
        if (boss == null) yield break;

        // 1 frame bekle: tüm Start() fonksiyonlarý çalýþsýn
        yield return null;

        // HP sabitle (EnemyHealth'te SetMaxHealth varsa en temiz yol)
        EnemyHealth eh = boss.GetComponent<EnemyHealth>();
        if (eh != null)
        {
            eh.SetMaxHealth(fixedBossHealth, true);
        }
        else
        {
            Debug.LogWarning("[BossWaveSpawner] Boss prefabýnda EnemyHealth yok!");
        }

        // Hasar sabitle
        SimpleEnemyFollow follow = boss.GetComponent<SimpleEnemyFollow>();
        if (follow != null)
        {
            follow.damage = fixedBossDamage;
        }
        else
        {
            Debug.LogWarning("[BossWaveSpawner] Boss prefabýnda SimpleEnemyFollow yok!");
        }
    }

    private bool TryGetGroundedSpawnPos(out Vector3 finalPos)
    {
        for (int t = 0; t < maxTriesPerBoss; t++)
        {
            Vector2 dir = Random.insideUnitCircle.normalized;
            float radius = Random.Range(innerRadius, outerRadius);

            Vector3 basePos = player.position + new Vector3(dir.x, 0f, dir.y) * radius;
            Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                finalPos = hit.point;
                return true;
            }
        }

        finalPos = Vector3.zero;
        return false;
    }
}
