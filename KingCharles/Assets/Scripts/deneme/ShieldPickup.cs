using UnityEngine;

public class ShieldPickup : MonoBehaviour
{
    [Header("Buff")]
    public float duration = 10f;

    [Header("Görsel (Hover + Spin)")]
    public float hoverHeight = 0.8f;
    public float bobAmplitude = 0.15f;
    public float bobFrequency = 2f;
    public float rotateSpeed = 60f;

    [Header("Mıknatıs / Toplama")]
    public string playerTag = "Animal";
    public float magnetRadius = 6f;
    public float magnetSpeed = 12f;
    public float collectDistance = 1.2f;
    public float flyToPlayerHeightOffset = 1.0f;

    private Transform player;
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
        if (pObj != null) player = pObj.transform;
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
                // ✅ Her yeni shield alınca süre tekrar 10 saniye
                GlobalShield.ActivateGlobal(duration);
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
}