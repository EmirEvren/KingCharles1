using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerXPUI : MonoBehaviour
{
    [Header("Referanslar")]
    public PlayerXP playerXP;          // Oyuncudaki PlayerXP scripti
    public TMP_Text levelText;         // "Lv 5" gibi yazacak text
    public Slider xpSlider;            // XP bar (0-1 arasý dolduracaðýz)

    private void Start()
    {
        // Inspector'dan atamazsan otomatik bulmayý dene
        if (playerXP == null)
        {
            // Yeni Unity versiyonlarý için önerilen yöntem
            playerXP = FindAnyObjectByType<PlayerXP>();

            if (playerXP == null)
            {
                Debug.LogWarning("[PlayerXPUI] Sahne içinde PlayerXP bulunamadý!");
            }
        }

        if (xpSlider != null)
        {
            xpSlider.minValue = 0f;
            xpSlider.maxValue = 1f;
        }
    }

    private void Update()
    {
        if (playerXP == null) return;

        // Level yazýsý
        if (levelText != null)
        {
            levelText.text = $"Lv {playerXP.level}";
        }

        // XP bar (0 - 1 arasý normalize)
        if (xpSlider != null && playerXP.xpToNextLevel > 0)
        {
            float ratio = (float)playerXP.currentXP / playerXP.xpToNextLevel;
            xpSlider.value = Mathf.Clamp01(ratio);
        }
    }
}
