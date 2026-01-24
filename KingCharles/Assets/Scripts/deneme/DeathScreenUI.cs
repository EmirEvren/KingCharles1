using UnityEngine;
using TMPro;
using MalbersAnimations;
using MalbersAnimations.Scriptables;
using UnityEngine.Localization; // <-- Şart

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance;

    [Header("UI")]
    public GameObject rootPanel;     
    public TMP_Text titleText;       // "ÖLDÜN" yazan text
    public TMP_Text killText;        // "Kill Count" yazan text

    [Header("Localization")]
    public LocalizedString killLabelKey;  // Kill Count Key'i
    public LocalizedString titleLabelKey; // <-- YENİ EKLENDİ (You Died Key'i)

    [Header("Player")]
    public string playerTag = "Animal";  
    public StatID healthID;              

    private Stats playerStats;
    private bool shown = false;

    private float prevTimeScale = 1f;
    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (rootPanel != null) rootPanel.SetActive(false);
        CachePlayer();
    }

    private void CachePlayer()
    {
        GameObject p = GameObject.FindGameObjectWithTag(playerTag);
        if (p == null) return;

        playerStats = p.GetComponentInChildren<Stats>();
        if (playerStats == null)
            playerStats = p.GetComponentInParent<Stats>();
    }

    private void Update()
    {
        if (shown) return;

        if (playerStats == null)
        {
            CachePlayer();
            return;
        }

        if (healthID == null) return;

        Stat hp = playerStats.Stat_Get(healthID);
        if (hp == null) return;

        if (hp.Value <= 0f)
        {
            ShowDeathPanel();
        }
    }

    private void ShowDeathPanel()
    {
        if (shown) return;
        shown = true;

        // --- 1. TITLE (BAŞLIK) ÇEVİRİSİ ---
        if (titleText != null)
        {
            // Tablodan "YOU DIED" / "ÖLDÜN" çevirisini alıp basıyoruz
            titleText.text = titleLabelKey.GetLocalizedString();
        }

        // --- 2. KILL COUNT ÇEVİRİSİ ---
        int kills = 0;
        if (KillCounterUI.Instance != null)
            kills = KillCounterUI.Instance.GetKillCount();

        if (killText != null)
        {
            string translatedLabel = killLabelKey.GetLocalizedString();
            killText.text = $"{translatedLabel}: {kills}";
        }

        PauseGameAndShowCursor();

        if (rootPanel != null)
            rootPanel.SetActive(true);
    }

    private void PauseGameAndShowCursor()
    {
        prevTimeScale = Time.timeScale;
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void HideAndResume()
    {
        if (!shown) return;

        if (rootPanel != null)
            rootPanel.SetActive(false);

        Time.timeScale = prevTimeScale;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockMode;
    }
}