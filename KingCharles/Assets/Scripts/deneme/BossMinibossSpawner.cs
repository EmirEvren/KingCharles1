using System.Collections;
using UnityEngine;

public class BossMinibossSpawner : MonoBehaviour
{
    [Header("MiniBoss")]
    public GameObject miniBossPrefab;

    [Header("Timing")]
    public float intervalSeconds = 30f;
    public int spawnCount = 2;

    [Header("Spawn Area")]
    public float spawnRadius = 10f;
    public LayerMask groundMask = ~0;
    public float groundRayHeight = 50f;
    public float groundRayDistance = 200f;

    private EnemyHealth enemyHealth;
    private Coroutine routine;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
    }

    private void OnEnable()
    {
        routine = StartCoroutine(Loop());
    }

    private void OnDisable()
    {
        if (routine != null) StopCoroutine(routine);
    }

    private IEnumerator Loop()
    {
        WaitForSeconds wait = new WaitForSeconds(intervalSeconds);

        while (true)
        {
            yield return wait;

            // Boss öldüyse dur
            if (enemyHealth != null && enemyHealth.GetCurrentHealth() <= 0f)
                yield break;

            SpawnMiniBosses();
        }
    }

    private void SpawnMiniBosses()
    {
        if (miniBossPrefab == null) return;

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 r = Random.insideUnitCircle * spawnRadius;
            Vector3 basePos = transform.position + new Vector3(r.x, 0f, r.y);

            Vector3 rayOrigin = basePos + Vector3.up * groundRayHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
                basePos = hit.point;

            Instantiate(miniBossPrefab, basePos, Quaternion.identity);
        }
    }
}
