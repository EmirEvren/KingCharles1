using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    [Header("Seviye Ayarları")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 10;          // İlk seviye için gereken XP
    public float xpGrowthMultiplier = 1.5f; // Her level sonrası XP ihtiyacı ne kadar artsın?

    [Header("Debug")]
    public bool logLevelUps = true;

    /// <summary>
    /// XP ekler ve gerekirse level up yapar.
    /// Level up olduğunda WeaponChoiceManager'a haber verir.
    /// </summary>
    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;

        // Birden fazla seviye atlayabilsin diye while
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;

            // Yeni level için XP sınırını arttır
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthMultiplier);

            if (logLevelUps)
                Debug.Log($"[PlayerXP] Level UP! Yeni Level: {level}, Sonraki level için XP: {xpToNextLevel}");

            // --- ÖNEMLİ KISIM: WeaponChoiceManager'a haber ver ---
            if (WeaponChoiceManager.Instance != null)
            {
                WeaponChoiceManager.Instance.OnPlayerLevelUp(level);
            }
            else
            {
                Debug.LogWarning("[PlayerXP] WeaponChoiceManager.Instance bulunamadı, level up kartı açılamadı!");
            }
            // ------------------------------------------------------
        }

        // Burada istersen XP bar / level text UI güncellemesi yapabilirsin
    }
}
