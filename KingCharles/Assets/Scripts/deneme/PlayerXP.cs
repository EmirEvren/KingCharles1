using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    [Header("Seviye Ayarları")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 10;
    public float xpGrowthMultiplier = 1.5f;

    [Header("Debug")]
    public bool logLevelUps = true;

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        // ---- XP GAIN RATE BONUS ----
        if (PlayerPermanentUpgrades.Instance != null)
        {
            amount = PlayerPermanentUpgrades.Instance.ModifyXP(amount);
        }
        // ----------------------------

        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;

            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthMultiplier);

            if (logLevelUps)
                Debug.Log($"[PlayerXP] Level UP! Yeni Level: {level}, Sonraki level için XP: {xpToNextLevel}");

            if (WeaponChoiceManager.Instance != null)
            {
                WeaponChoiceManager.Instance.OnPlayerLevelUp(level);
            }
            else
            {
                Debug.LogWarning("[PlayerXP] WeaponChoiceManager.Instance bulunamadı, level up kartı açılamadı!");
            }
        }
    }
}
