using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class GlobalShield : MonoBehaviour
{
    public static GlobalShield Instance;

    [Header("Player")]
    public string playerTag = "Animal";
    public StatID healthStatID; // ✅ Health StatID'ni buraya ver

    [Header("Defaults")]
    public float defaultDuration = 10f;

    private float endTime = -1f;

    private Stats playerStats;
    private bool appliedByShield = false; // sadece biz açtıysak kapatalım

    public bool IsActive => Time.time < endTime;
    public float Remaining => Mathf.Max(0f, endTime - Time.time);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        EnsurePlayer();

        // Shield aktifken: her frame immune zorla (Stat'ın ImmuneTime coroutine'i bozamasın)
        if (IsActive)
        {
            if (playerStats != null && healthStatID != null)
            {
                playerStats.Stat_Immune_Activate(healthStatID);
                appliedByShield = true;
            }
            return;
        }

        // Shield bittiğinde: sadece biz açtıysak kapat
        if (appliedByShield)
        {
            if (playerStats != null && healthStatID != null)
            {
                playerStats.Stat_Immune_Deactivate(healthStatID);
            }
            appliedByShield = false;
        }
    }

    private void EnsurePlayer()
    {
        if (playerStats != null) return;

        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p == null) return;

        playerStats = p.GetComponent<Stats>();
        if (playerStats == null) playerStats = p.GetComponentInChildren<Stats>();
        if (playerStats == null) playerStats = p.GetComponentInParent<Stats>();
    }

    public void Activate(float duration)
    {
        endTime = Time.time + Mathf.Max(0.01f, duration);
    }

    public static void ActivateGlobal(float duration = 10f)
    {
        if (Instance == null)
        {
            var go = new GameObject("GlobalShield");
            Instance = go.AddComponent<GlobalShield>();
        }

        Instance.Activate(duration);
    }

    private void OnDisable()
    {
        // Sahne kapanırken açık kaldıysa kapat
        if (appliedByShield && playerStats != null && healthStatID != null)
        {
            playerStats.Stat_Immune_Deactivate(healthStatID);
        }
        appliedByShield = false;
    }

    private void OnDestroy()
    {
        if (appliedByShield && playerStats != null && healthStatID != null)
        {
            playerStats.Stat_Immune_Deactivate(healthStatID);
        }
        appliedByShield = false;
    }
}