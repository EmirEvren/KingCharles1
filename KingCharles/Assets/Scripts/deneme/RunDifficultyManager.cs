using System;
using UnityEngine;

public class RunDifficultyManager : MonoBehaviour
{
    public static RunDifficultyManager Instance;

    [Header("Difficulty Scaling")]
    [Tooltip("Her dakika zorluk çarpaný (1.2 = %20 artýþ)")]
    public float perMinuteMultiplier = 1.2f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int GetBaseMinutes()
    {
        float t = (GameTimerUI.Instance != null) ? GameTimerUI.Instance.GetElapsedTime() : Time.timeSinceLevelLoad;
        return Mathf.FloorToInt(t / 60f);
    }

    public int GetBonusMinutesFromItems()
    {
        if (PlayerPermanentUpgrades.Instance == null) return 0;
        return PlayerPermanentUpgrades.Instance.difficultyBonusMinutes;
    }

    public int GetEffectiveMinutes()
    {
        return Mathf.Max(0, GetBaseMinutes() + GetBonusMinutesFromItems());
    }

    public float GetCurrentMultiplier()
    {
        int m = GetEffectiveMinutes();

        // double ile hesapla, float overflow olursa clamp
        double val = Math.Pow(perMinuteMultiplier, m);
        if (val > float.MaxValue) return float.MaxValue;
        return (float)val;
    }
}
