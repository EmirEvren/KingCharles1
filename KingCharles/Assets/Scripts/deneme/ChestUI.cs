using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChestUI : MonoBehaviour
{
    public static ChestUI Instance;

    [Header("Panel")]
    public GameObject rootPanel;

    [Header("Reward Button")]
    public Button rewardButton;
    public Image rewardIcon;
    public TMP_Text rewardNameText;

    [Header("Rarity Colors (Button)")]
    public Color commonColor = new Color(0.75f, 0.75f, 0.75f, 1f);     // gri
    public Color uncommonColor = new Color(0.30f, 0.85f, 0.35f, 1f);   // yeþil
    public Color rareColor = new Color(0.30f, 0.55f, 0.95f, 1f);       // mavi
    public Color epicColor = new Color(0.65f, 0.30f, 0.95f, 1f);       // mor
    public Color legendaryColor = new Color(0.95f, 0.80f, 0.15f, 1f);  // sarý

    private Action onClaim;

    // UI açýldýðýnda eski state'i geri yüklemek için cache
    private float prevTimeScale = 1f;
    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;

    private bool pausedByChestUI = false;

    private void Awake()
    {
        Instance = this;
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    public void ShowReward(ChestReward reward, Action onClaimCallback)
    {
        onClaim = onClaimCallback;

        // --- OYUNU DURDUR + CURSOR AÇ ---
        PauseGameAndShowCursor();
        // ------------------------------

        if (rewardIcon != null) rewardIcon.sprite = reward.icon;

        if (rewardNameText != null)
        {
            // Legendary-only ise value yazmak istemezsen sadece isim býrak
            string suffix = (reward.value > 0) ? $" +{reward.value}" : "";
            rewardNameText.text = $"{reward.displayName}{suffix}";
        }

        // --- RARITY RENK ---
        ApplyRarityColor(reward);
        // -------------------

        if (rewardButton != null)
        {
            rewardButton.onClick.RemoveAllListeners();
            rewardButton.onClick.AddListener(Claim);
        }

        if (rootPanel != null) rootPanel.SetActive(true);
    }

    private void Claim()
    {
        if (rootPanel != null) rootPanel.SetActive(false);

        // --- OYUNU DEVAM ETTÝR + CURSOR ESKÝ HALÝNE ---
        ResumeGameAndRestoreCursor();
        // ---------------------------------------------

        onClaim?.Invoke();
        onClaim = null;
    }

    private void PauseGameAndShowCursor()
    {
        if (pausedByChestUI) return;
        pausedByChestUI = true;

        prevTimeScale = Time.timeScale;
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        Time.timeScale = 0f;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ResumeGameAndRestoreCursor()
    {
        if (!pausedByChestUI) return;
        pausedByChestUI = false;

        Time.timeScale = prevTimeScale;

        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockMode;
    }

    private void ApplyRarityColor(ChestReward reward)
    {
        if (rewardButton == null) return;

        Image btnImg = rewardButton.GetComponent<Image>();
        if (btnImg == null) return;

        Color c = commonColor;

        // DÝKKAT: Burada ChestRarity kullanýyoruz (UpgradeRarity deðil)
        switch (reward.rarity)
        {
            case ChestRarity.Common:
                c = commonColor; break;
            case ChestRarity.Uncommon:
                c = uncommonColor; break;
            case ChestRarity.Rare:
                c = rareColor; break;
            case ChestRarity.Epic:
                c = epicColor; break;
            case ChestRarity.Legendary:
                c = legendaryColor; break;
        }

        btnImg.color = c;
    }

    // Güvenlik: UI sahneden silinirse oyun kilitli kalmasýn
    private void OnDisable()
    {
        if (pausedByChestUI)
        {
            ResumeGameAndRestoreCursor();
        }
    }
}
