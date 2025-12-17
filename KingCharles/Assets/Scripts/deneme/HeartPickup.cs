using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class HeartPickup : MonoBehaviour
{
    [Header("Görsel (Hover + Spin)")]
    public float hoverHeight = 0.8f;
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;
    public float rotateSpeed = 60f;

    [Header("Mýknatýs / Toplama")]
    public string playerTag = "Animal";
    public float magnetRadius = 6f;
    public float magnetSpeed = 12f;
    public float collectDistance = 1.2f;
    public float flyToPlayerHeightOffset = 1.0f;

    [Header("Heal")]
    [Range(0f, 1f)]
    public float healPercentOfMax = 0.20f;
    public StatID healthID;

    private Transform player;
    private Stats playerStats;

    private Vector3 basePos;
    private bool isMagneting;

    private void Awake()
    {
        basePos = transform.position;
        TryFindPlayer();
    }

    private void TryFindPlayer()
    {
        GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
        if (pObj == null) return;

        player = pObj.transform;

        playerStats = pObj.GetComponent<Stats>();
        if (playerStats == null)
            playerStats = pObj.GetComponentInParent<Stats>();
    }

    private void Update()
    {
        if (player == null)
        {
            TryFindPlayer();
            IdleVisual();
            return;
        }

        transform.Rotate(0f, rotateSpeed * Time.deltaTime, 0f, Space.Self);

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        float sqrDist = toPlayer.sqrMagnitude;

        if (!isMagneting && sqrDist <= magnetRadius * magnetRadius)
            isMagneting = true;

        if (isMagneting)
        {
            Vector3 targetPos = player.position + Vector3.up * flyToPlayerHeightOffset;
            transform.position = Vector3.MoveTowards(transform.position, targetPos, magnetSpeed * Time.deltaTime);

            if ((transform.position - targetPos).sqrMagnitude <= collectDistance * collectDistance)
            {
                ApplyHeal();
                Destroy(gameObject);
            }
        }
        else
        {
            IdleVisual();
        }
    }

    private void IdleVisual()
    {
        float y = basePos.y + hoverHeight + Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        Vector3 p = transform.position;
        p.y = y;
        transform.position = p;
    }

    private void ApplyHeal()
    {
        if (playerStats == null || healthID == null)
        {
            Debug.LogWarning("[HeartPickup] playerStats veya healthID yok. Heal uygulanamadý.");
            return;
        }

        Stat hp = playerStats.Stat_Get(healthID);
        if (hp == null)
        {
            Debug.LogWarning("[HeartPickup] Stat_Get(healthID) null döndü. healthID yanlýþ olabilir.");
            return;
        }

        float healAmount = hp.MaxValue * healPercentOfMax;

        // Malbers Stat: mevcut deðer "Value"
        hp.Value = Mathf.Min(hp.MaxValue, hp.Value + healAmount);
    }
}
