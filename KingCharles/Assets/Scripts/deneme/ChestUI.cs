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
    public Color uncommonColor = new Color(0.30f, 0.85f, 0.35f, 1f);   // yeşil
    public Color rareColor = new Color(0.30f, 0.55f, 0.95f, 1f);       // mavi
    public Color epicColor = new Color(0.65f, 0.30f, 0.95f, 1f);       // mor
    public Color legendaryColor = new Color(0.95f, 0.80f, 0.15f, 1f);  // sarı

    [Header("Seçim Açılınca Durdurulacak Scriptler (Opsiyonel)")]
    public MonoBehaviour[] scriptsToDisableWhileChoosing;
    private bool[] prevScriptStates;

    [Header("SADECE BUNLAR GİZLENECEK (Senin Seçtiklerin)")]
    public GameObject[] uiElementsToHide;
    private bool[] prevUIStates;

    private Action onClaim;

    // UI açıldığında eski state'i geri yüklemek için cache
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

        // --- OYUNU DURDUR + CURSOR AÇ + SEÇTİĞİN UI'LARI GİZLE ---
        PauseGameAndShowCursor();
        // ------------------------------

        if (rewardIcon != null) rewardIcon.sprite = reward.icon;

        if (rewardNameText != null)
        {
            // Legendary-only ise value yazmak istemezsen sadece isim bırak
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

        // --- OYUNU DEVAM ETTİR + CURSOR ESKİ HALİNE + GİZLENEN UI'LARI GERİ AÇ ---
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

        // --- SCRİPTLERİ DEVRE DIŞI BIRAK ---
        if (scriptsToDisableWhileChoosing != null && scriptsToDisableWhileChoosing.Length > 0)
        {
            prevScriptStates = new bool[scriptsToDisableWhileChoosing.Length];
            for (int i = 0; i < scriptsToDisableWhileChoosing.Length; i++)
            {
                if (scriptsToDisableWhileChoosing[i] != null)
                {
                    prevScriptStates[i] = scriptsToDisableWhileChoosing[i].enabled;
                    scriptsToDisableWhileChoosing[i].enabled = false;
                }
            }
        }

        // --- SADECE SENİN SEÇTİĞİN UI'LARI GİZLE ---
        if (uiElementsToHide != null && uiElementsToHide.Length > 0)
        {
            prevUIStates = new bool[uiElementsToHide.Length];
            for (int i = 0; i < uiElementsToHide.Length; i++)
            {
                if (uiElementsToHide[i] != null)
                {
                    // Şu anki durumunu kaydet (zaten kapalı olanı açmamak için)
                    prevUIStates[i] = uiElementsToHide[i].activeSelf; 
                    
                    // UI'ı gizle
                    uiElementsToHide[i].SetActive(false); 
                }
            }
        }
    }

    private void ResumeGameAndRestoreCursor()
    {
        if (!pausedByChestUI) return;
        pausedByChestUI = false;

        Time.timeScale = prevTimeScale;

        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockMode;

        // --- SCRİPTLERİ ESKİ HALİNE GETİR ---
        if (scriptsToDisableWhileChoosing != null && prevScriptStates != null)
        {
            for (int i = 0; i < scriptsToDisableWhileChoosing.Length; i++)
            {
                if (scriptsToDisableWhileChoosing[i] != null && i < prevScriptStates.Length)
                {
                    scriptsToDisableWhileChoosing[i].enabled = prevScriptStates[i];
                }
            }
        }

        // --- GİZLENEN UI'LARI ESKİ DURUMUNA GETİR ---
        if (uiElementsToHide != null && prevUIStates != null)
        {
            for (int i = 0; i < uiElementsToHide.Length; i++)
            {
                if (uiElementsToHide[i] != null && i < prevUIStates.Length)
                {
                    uiElementsToHide[i].SetActive(prevUIStates[i]); 
                }
            }
        }
    }

    private void ApplyRarityColor(ChestReward reward)
    {
        if (rewardButton == null) return;

        Image btnImg = rewardButton.GetComponent<Image>();
        if (btnImg == null) return;

        Color c = commonColor;

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

    private void OnDisable()
    {
        if (pausedByChestUI)
        {
            ResumeGameAndRestoreCursor();
        }
    }
}