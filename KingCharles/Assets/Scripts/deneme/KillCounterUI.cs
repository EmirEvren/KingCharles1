using UnityEngine;
using TMPro;

public class KillCounterUI : MonoBehaviour
{
    public static KillCounterUI Instance;

    [Header("UI Referansý")]
    public TMP_Text killText;   // Canvas üzerindeki TMP_Text'i buraya sürükle

    private int killCount = 0;

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
        killCount = 0;
        RefreshText();
    }

    private void RefreshText()
    {
        if (killText != null)
        {
            // Ýstersen sadece killCount.ToString() da yazdýrabilirsin
            killText.text = $": {killCount}";
        }
    }

    public void AddKill()
    {
        killCount++;
        RefreshText();
    }

    // EnemyHealth içinden statik çaðrý kullanmak için:
    public static void RegisterKill()
    {
        if (Instance != null)
        {
            Instance.AddKill();
        }
        else
        {
            Debug.LogWarning("[KillCounterUI] Sahnede Instance yok, KillCounterUI objesini ekleyip killText referansýný doldur.");
        }
    }
}
