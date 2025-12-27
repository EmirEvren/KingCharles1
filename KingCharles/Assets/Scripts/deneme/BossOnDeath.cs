using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class BossOnDeath : MonoBehaviour
{
    [Header("Player")]
    public string playerTag = "Animal";
    public StatID healthStatID;

    [Header("Spawn On Death")]
    public GameObject statuePrefab;
    public LayerMask groundMask = ~0;
    public float groundRayHeight = 50f;
    public float groundRayDistance = 200f;

    private EnemyHealth enemyHealth;
    private Stats playerStats;
    private bool handled = false;

    private void Awake()
    {
        enemyHealth = GetComponent<EnemyHealth>();
        CachePlayer();
    }

    private void CachePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p != null)
            playerStats = p.GetComponentInChildren<Stats>();
    }

    private void Update()
    {
        if (handled) return;
        if (enemyHealth == null) return;

        if (enemyHealth.GetCurrentHealth() <= 0f)
        {
            handled = true;
            HealPlayerToFull();
            SpawnStatue();
        }
    }

    private void HealPlayerToFull()
    {
        if (playerStats == null) return;
        if (healthStatID == null) return;

        // Büyük pozitif ver → Max’a clamp eder
        playerStats.Stat_ModifyValue(healthStatID, 999999f);
    }

    private void SpawnStatue()
    {
        if (statuePrefab == null) return;

        Vector3 pos = transform.position;
        Vector3 rayOrigin = pos + Vector3.up * groundRayHeight;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore))
            pos = hit.point;

        Instantiate(statuePrefab, pos, Quaternion.identity);
    }
}
