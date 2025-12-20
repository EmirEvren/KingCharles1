using UnityEngine;
using TMPro;

public class GoldCounterUI : MonoBehaviour
{
    public static GoldCounterUI Instance;

    [Header("UI Referansý")]
    public TMP_Text goldText;

    private int goldCount = 0;

    public int GetGold() => goldCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool TrySpend(int amount)
    {
        if (amount <= 0) return true;
        if (goldCount < amount) return false;

        goldCount -= amount;
        if (goldCount < 0) goldCount = 0;
        RefreshText();
        return true;
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

    public static void RegisterGold(int amount)
    {
        // ---- GOLD GAIN RATE BONUS ----
        if (PlayerPermanentUpgrades.Instance != null)
        {
            amount = PlayerPermanentUpgrades.Instance.ModifyGold(amount);
        }
        // -----------------------------

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
