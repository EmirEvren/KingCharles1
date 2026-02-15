using UnityEngine;
using TMPro;

public class KillCounterUI : MonoBehaviour
{
    public static KillCounterUI Instance;

    [Header("UI Reference")]
    [SerializeField] private TMP_Text killText;

    private int killCount = 0;

    public int GetKillCount() => killCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        RefreshText(); // UI açıldığında güncel değeri yaz
    }

    private void Start()
    {
        ResetKills();
    }

    // --------------------------------------------------
    // KILL EKLEME
    // --------------------------------------------------
    public void AddKill()
    {
        killCount++;
        RefreshText();
    }

    public static void RegisterKill()
    {
        if (Instance != null)
        {
            Instance.AddKill();
        }
        else
        {
            Debug.LogWarning("[KillCounterUI] Instance bulunamadı.");
        }
    }

    // --------------------------------------------------
    // RESET
    // --------------------------------------------------
    public void ResetKills()
    {
        killCount = 0;
        RefreshText();
    }

    // --------------------------------------------------
    // UI UPDATE
    // --------------------------------------------------
    private void RefreshText()
    {
        if (killText == null)
        {
            Debug.LogWarning("[KillCounterUI] killText atanmadı!");
            return;
        }

        killText.text = $": {killCount}";
    }
}
