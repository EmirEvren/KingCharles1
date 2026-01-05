using UnityEngine;
using TMPro;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class DeathScreenUI : MonoBehaviour
{
    public static DeathScreenUI Instance;

    [Header("UI")]
    public GameObject rootPanel;     // DeathPanel
    public TMP_Text titleText;       // "ÖLDÜN"
    public TMP_Text killText;        // "Kill Count: X"

    [Header("Player")]
    public string playerTag = "Animal";  // Senin player tag'in
    public StatID healthID;              // Player'ýn Health StatID'si

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

        if (titleText != null)
            titleText.text = "You DIED";

        int kills = 0;
        if (KillCounterUI.Instance != null)
            kills = KillCounterUI.Instance.GetKillCount();

        if (killText != null)
            killText.text = $"Kill Count: {kills}";

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

    // Ýleride restart/continue eklemek istersen diye hazýr:
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
