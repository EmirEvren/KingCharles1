using UnityEngine;
using TMPro;

public class GoldCounterUI : MonoBehaviour
{
    public static GoldCounterUI Instance;

    [Header("UI Referansý")]
    public TMP_Text goldText;   // Canvas üzerindeki TMP_Text'i buraya sürükle

    private int goldCount = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        goldCount = 0;
        RefreshText();
    }

    private void RefreshText()
    {
        if (goldText != null)
        {
            goldText.text = $": {goldCount}";
        }
    }

    public void AddGold(int amount)
    {
        goldCount += amount;
        if (goldCount < 0) goldCount = 0;
        RefreshText();
    }

    // Pickup'larýn rahat çaðýrabilmesi için static helper
    public static void RegisterGold(int amount)
    {
        // ---- GOLD GAIN RATE BONUS (DogHouse upgrade) ----
        if (PlayerPermanentUpgrades.Instance != null)
        {
            amount = PlayerPermanentUpgrades.Instance.ModifyGold(amount);
        }
        // -------------------------------------------------

        if (Instance != null)
        {
            Instance.AddGold(amount);
        }
        else
        {
            Debug.LogWarning("[GoldCounterUI] Sahnede Instance yok, GoldCounterUI objesini ekleyip goldText referansýný doldur.");
        }
    }
}
