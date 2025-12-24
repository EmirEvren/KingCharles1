using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StatueUI : MonoBehaviour
{
    public static StatueUI Instance;

    [Header("Panel")]
    public GameObject rootPanel;

    [Header("Buttons")]
    public Button button1;
    public Button button2;
    public Button button3;

    [Header("Icons")]
    public Image icon1;
    public Image icon2;
    public Image icon3;

    [Header("Texts")]
    public TMP_Text text1;
    public TMP_Text text2;
    public TMP_Text text3;

    [Header("Rarity Colors (Button)")]
    public Color commonColor = new Color(0.75f, 0.75f, 0.75f, 1f);     // gri
    public Color uncommonColor = new Color(0.30f, 0.85f, 0.35f, 1f);   // yeþil
    public Color rareColor = new Color(0.30f, 0.55f, 0.95f, 1f);       // mavi
    public Color epicColor = new Color(0.65f, 0.30f, 0.95f, 1f);       // mor
    public Color legendaryColor = new Color(0.95f, 0.80f, 0.15f, 1f);  // sarý

    private Action<ChestReward> onPick;

    // UI açýldýðýnda eski state'i geri yüklemek için cache
    private float prevTimeScale = 1f;
    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;
    private bool pausedByUI = false;

    private void Awake()
    {
        Instance = this;
        if (rootPanel != null) rootPanel.SetActive(false);
    }

    public void ShowChoices(List<ChestReward> rewards, Action<ChestReward> onPickCallback)
    {
        if (rewards == null || rewards.Count < 3)
        {
            Debug.LogWarning("[StatueUI] rewards listesi 3 eleman deðil!");
            return;
        }

        onPick = onPickCallback;

        PauseGameAndShowCursor();

        if (rootPanel != null) rootPanel.SetActive(true);

        SetupSlot(1, rewards[0]);
        SetupSlot(2, rewards[1]);
        SetupSlot(3, rewards[2]);
    }

    private void SetupSlot(int index, ChestReward reward)
    {
        Button btn = null;
        Image img = null;
        TMP_Text txt = null;

        if (index == 1) { btn = button1; img = icon1; txt = text1; }
        else if (index == 2) { btn = button2; img = icon2; txt = text2; }
        else { btn = button3; img = icon3; txt = text3; }

        if (img != null) img.sprite = reward.icon;

        if (txt != null)
        {
            string suffix = (reward.value > 0) ? $" +{reward.value}" : "";
            txt.text = $"{reward.displayName}{suffix}";
        }

        if (btn != null)
        {
            // rarity rengi butona uygula
            ApplyRarityColor(btn, reward.rarity);

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => Pick(reward));
        }
    }

    private void Pick(ChestReward chosen)
    {
        if (rootPanel != null) rootPanel.SetActive(false);

        ResumeGameAndRestoreCursor();

        onPick?.Invoke(chosen);
        onPick = null;
    }

    private void ApplyRarityColor(Button btn, ChestRarity rarity)
    {
        Image btnImg = btn.GetComponent<Image>();
        if (btnImg == null) return;

        Color c = commonColor;

        switch (rarity)
        {
            case ChestRarity.Common: c = commonColor; break;
            case ChestRarity.Uncommon: c = uncommonColor; break;
            case ChestRarity.Rare: c = rareColor; break;
            case ChestRarity.Epic: c = epicColor; break;
            case ChestRarity.Legendary: c = legendaryColor; break;
        }

        btnImg.color = c;
    }

    private void PauseGameAndShowCursor()
    {
        if (pausedByUI) return;
        pausedByUI = true;

        prevTimeScale = Time.timeScale;
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ResumeGameAndRestoreCursor()
    {
        if (!pausedByUI) return;
        pausedByUI = false;

        Time.timeScale = prevTimeScale;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockMode;
    }

    private void OnDisable()
    {
        if (pausedByUI)
        {
            ResumeGameAndRestoreCursor();
        }
    }
}
