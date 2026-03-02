using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Localization.Settings;
using Steamworks;
using UnityEngine.AI; // ✅ NavMeshAgent için (varsa)

public class MainMenuManager : MonoBehaviour
{
    public static bool RestartIstendi = false;

    [Header("--- UI PANELLERİ ---")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject multiplayerPanel;

    [Header("--- OYUN DÜNYASI ---")]
    public GameObject gameWorldContainer;
    public MeabunkMapGenerator mapGeneratorScript;

    [Header("--- KAMERALAR ---")]
    public GameObject menuCameraObj;
    public GameObject cmBrainCameraObj;

    [Header("--- AYARLAR ---")]
    public bool generateMapOnPlay = true;

    [Header("--- SOSYAL MEDYA ---")]
    public string discordLink = "https://discord.gg/CFdn96Wb";
    public string youtubeLink = "https://www.youtube.com/@realify-games";

    [Header("--- DİL SİSTEMİ ---")]
    public Image languageFlagImage;
    public TextMeshProUGUI languageNameText;

    public List<LanguageData> languages;

    private int currentLanguageIndex = 0;
    private const string PREF_LANGUAGE_INDEX = "SelectedLanguageIndex";
    private bool isChangingLanguage = false;

    [System.Serializable]
    public struct LanguageData
    {
        public string languageName;
        public Sprite flagSprite;
        public string languageCode;
    }

    [Header("--- PLAYER START POINTS ---")]
    [Tooltip("Player Play'e basınca bu noktalardan birine IŞINLANACAK. (Instantiate yok)")]
    public Transform[] playerStartPoints;

    [Tooltip("Player'ı tag ile bulmak için. Senin oyunda Animal.")]
    public string playerTag = "Animal";

    [Tooltip("Eğer player NavMeshAgent kullanıyorsa Warp denensin.")]
    public bool warpNavMeshAgentIfExists = true;

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        LoadLanguageSettings();

        if (RestartIstendi)
        {
            RestartIstendi = false;
            KillCounterUI.Instance?.ResetKills();
            OnPlayClicked();
        }
        else
        {
            gameWorldContainer.SetActive(false);
            if (cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
            if (menuCameraObj != null) menuCameraObj.SetActive(true);

            ShowMainMenu();
        }
    }

    private void LoadLanguageSettings()
    {
        currentLanguageIndex = PlayerPrefs.GetInt(PREF_LANGUAGE_INDEX, 0);

        if (languages.Count > 0 && currentLanguageIndex >= languages.Count)
            currentLanguageIndex = 0;

        UpdateLanguageUI();
        StartCoroutine(SetLocale(currentLanguageIndex));
    }

    public void OnLanguageToggleClicked()
    {
        if (languages.Count == 0 || isChangingLanguage) return;

        currentLanguageIndex++;
        if (currentLanguageIndex >= languages.Count)
            currentLanguageIndex = 0;

        PlayerPrefs.SetInt(PREF_LANGUAGE_INDEX, currentLanguageIndex);
        PlayerPrefs.Save();

        UpdateLanguageUI();
        StartCoroutine(SetLocale(currentLanguageIndex));
    }

    private void UpdateLanguageUI()
    {
        if (languages.Count == 0) return;

        LanguageData currentLang = languages[currentLanguageIndex];

        if (languageFlagImage != null)
            languageFlagImage.sprite = currentLang.flagSprite;

        if (languageNameText != null)
            languageNameText.text = currentLang.languageName;
    }

    IEnumerator SetLocale(int _index)
    {
        isChangingLanguage = true;

        string targetCode = languages[_index].languageCode;
        var locale = LocalizationSettings.AvailableLocales.GetLocale(targetCode);

        if (locale == null)
            locale = LocalizationSettings.AvailableLocales.Locales[_index];

        LocalizationSettings.SelectedLocale = locale;

        yield return null;
        isChangingLanguage = false;

        Debug.Log("Dil değiştirildi: " + locale.Identifier.Code);
    }

    public void OnDiscordClicked() { Application.OpenURL(discordLink); }
    public void OnYoutubeClicked() { Application.OpenURL(youtubeLink); }

    public void OnPlayClicked()
    {
        // ✅ KRİTİK: Silah seçimi gelene kadar oyunu durdur
        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        KillCounterUI.Instance?.ResetKills();

        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);

        gameWorldContainer.SetActive(true);

        if (menuCameraObj != null) menuCameraObj.SetActive(false);
        if (cmBrainCameraObj != null) cmBrainCameraObj.SetActive(true);

        TeleportPlayerToRandomStartPoint();

        if (generateMapOnPlay && mapGeneratorScript != null)
            mapGeneratorScript.SendMessage("GenerateMap", SendMessageOptions.DontRequireReceiver);
    }

    private void TeleportPlayerToRandomStartPoint()
    {
        if (playerStartPoints == null || playerStartPoints.Length == 0)
        {
            Debug.LogWarning("[MainMenuManager] playerStartPoints boş. Player teleport edilmedi.");
            return;
        }

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObj == null)
        {
            Debug.LogWarning($"[MainMenuManager] Player tag '{playerTag}' bulunamadı. Teleport yapılamadı.");
            return;
        }

        List<Transform> valid = new List<Transform>(playerStartPoints.Length);
        for (int i = 0; i < playerStartPoints.Length; i++)
        {
            if (playerStartPoints[i] != null) valid.Add(playerStartPoints[i]);
        }

        if (valid.Count == 0)
        {
            Debug.LogWarning("[MainMenuManager] playerStartPoints içinde hiç geçerli Transform yok.");
            return;
        }

        Transform chosen = valid[Random.Range(0, valid.Count)];

        if (warpNavMeshAgentIfExists)
        {
            NavMeshAgent agent = playerObj.GetComponent<NavMeshAgent>();
            if (agent == null) agent = playerObj.GetComponentInChildren<NavMeshAgent>();
            if (agent == null) agent = playerObj.GetComponentInParent<NavMeshAgent>();

            if (agent != null && agent.enabled)
            {
                agent.Warp(chosen.position);
                playerObj.transform.rotation = chosen.rotation;
                return;
            }
        }

        playerObj.transform.position = chosen.position;
        playerObj.transform.rotation = chosen.rotation;

        Rigidbody rb = playerObj.GetComponent<Rigidbody>();
        if (rb == null) rb = playerObj.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        Physics.SyncTransforms();
    }

    public void OnMultiplayerClicked()
    {
        mainMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
    }

    public void OnSettingsClicked()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void OnExitClicked() => Application.Quit();

    public void OnBackToMenuClicked()
    {
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        gameWorldContainer.SetActive(false);
        if (cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
        if (menuCameraObj != null) menuCameraObj.SetActive(true);
        mainMenuPanel.SetActive(true);

        if (SteamManager.Initialized && SteamLeaderboardManager.Instance != null)
        {
            SteamLeaderboardManager.Instance.DownloadTop10();
        }
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);

        if (SteamManager.Initialized && SteamLeaderboardManager.Instance != null)
        {
            SteamLeaderboardManager.Instance.DownloadTop10();
        }
    }
    // ===============================
    // ITEMS PANEL
    // ===============================
    [Header("--- ITEMS PANEL ---")]
    public GameObject itemsPanel;

    public void OnItemsClicked()
    {
        if (itemsPanel != null) itemsPanel.SetActive(true);
    }

    public void OnCloseItemsClicked()
    {
        if (itemsPanel != null) itemsPanel.SetActive(false);
    }
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (settingsPanel.activeSelf || multiplayerPanel.activeSelf)
            {
                OnBackToMenuClicked();
            }
        }
    }
}