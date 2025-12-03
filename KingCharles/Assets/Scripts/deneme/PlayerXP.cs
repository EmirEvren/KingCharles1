using UnityEngine;

public class PlayerXP : MonoBehaviour
{
    [Header("Seviye Ayarlarý")]
    public int level = 1;
    public int currentXP = 0;
    public int xpToNextLevel = 10;      // Ýlk seviye için gereken XP
    public float xpGrowthMultiplier = 1.5f; // Her level sonrasý XP ihtiyacý ne kadar artsýn?

    [Header("Debug")]
    public bool logLevelUps = true;

    public void AddXP(int amount)
    {
        if (amount <= 0) return;

        currentXP += amount;

        // Level up kontrolü (birden fazla seviye atlayabilsin diye while)
        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            level++;

            // Yeni level için XP sýnýrýný arttýr
            xpToNextLevel = Mathf.RoundToInt(xpToNextLevel * xpGrowthMultiplier);

            if (logLevelUps)
                Debug.Log($"[PlayerXP] Level UP! Yeni Level: {level}, Sonraki level için XP: {xpToNextLevel}");

            // Buraya level up olduðunda güç artýþý vs. ekleyebilirsin
            // Örn: movementSpeed++, damage++, maxHealth++ vs.
        }

        // Burada istersen UI güncelleyebilirsin (XP bar, level text vs.)
    }
}
