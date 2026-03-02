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
    // =========================================================
    // YENİ EKLENEN: RESTART KONTROLÜ
    // =========================================================
    // Bu değişken 'static' olduğu için sahne yenilense bile hafızada kalır.
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

    // ÖNEMLİ: Element 0'ı İngilizce yaparsan, oyun ilk açılışta (kayıt yoksa) İngilizce başlar.
    public List<LanguageData> languages;

    private int currentLanguageIndex = 0;
    private const string PREF_LANGUAGE_INDEX = "SelectedLanguageIndex";

    // Bayrak değişiminin çok hızlı olmasını engellemek için
    private bool isChangingLanguage = false;

    [System.Serializable]
    public struct LanguageData
    {
        public string languageName;
        public Sprite flagSprite;
        public string languageCode;
    }

    // =========================================================
    // ✅ EKLENDİ: PLAYER TELEPORT START POINTS
    // =========================================================
    [Header("--- PLAYER START POINTS ---")]
    [Tooltip("Player Play'e basınca bu noktalardan birine IŞINLANACAK. (Instantiate yok)")]
    public Transform[] playerStartPoints;

    [Tooltip("Player'ı tag ile bulmak için. Senin oyunda Animal.")]
    public string playerTag = "Animal";

    [Tooltip("Eğer player NavMeshAgent kullanıyorsa Warp denensin.")]
    public bool warpNavMeshAgentIfExists = true;

    // Start fonksiyonu hem Localization'ı bekler hem de Restart kontrolü yapar
    private IEnumerator Start()
    {
        // 1. Localization sisteminin hazır olmasını bekle
        yield return LocalizationSettings.InitializationOperation;

        // 2. Kayıtlı dili yükle
        LoadLanguageSettings();

        // =========================================================
        // RESTART MANTIĞI BURADA DEVREYE GİRİYOR
        // =========================================================
        if (RestartIstendi)
        {
            RestartIstendi = false;

            KillCounterUI.Instance?.ResetKills(); // 🔥 EKLE

            OnPlayClicked();
        }

        else
        {
            // Normal açılış (Oyun ilk açıldığında veya menüye dönüldüğünde)
            gameWorldContainer.SetActive(false);
            if (cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
            if (menuCameraObj != null) menuCameraObj.SetActive(true);

            ShowMainMenu();
        }
    }

    // =================================================
    //              DİL SİSTEMİ
    // =================================================

    private void LoadLanguageSettings()
    {
        // Kayıt yoksa 0 (Varsayılan) döner.
        currentLanguageIndex = PlayerPrefs.GetInt(PREF_LANGUAGE_INDEX, 0);

        if (languages.Count > 0 && currentLanguageIndex >= languages.Count)
            currentLanguageIndex = 0;

        // Hem UI'ı güncelle hem de Unity'nin dilini değiştir
        UpdateLanguageUI();
        StartCoroutine(SetLocale(currentLanguageIndex));
    }

    public void OnLanguageToggleClicked()
    {
        if (languages.Count == 0 || isChangingLanguage) return;

        currentLanguageIndex++;

        if (currentLanguageIndex >= languages.Count)
            currentLanguageIndex = 0;

        // Kaydet
        PlayerPrefs.SetInt(PREF_LANGUAGE_INDEX, currentLanguageIndex);
        PlayerPrefs.Save();

        // Görünümü güncelle
        UpdateLanguageUI();

        // Asıl Dili Değiştir (Localization)
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

    // Unity Localization Paketine "Dili Değiştir" emrini veren fonksiyon
    IEnumerator SetLocale(int _index)
    {
        isChangingLanguage = true;

        // Listemizdeki dil kodunu alıyoruz
        string targetCode = languages[_index].languageCode;

        // Localization sistemindeki uygun dili buluyoruz
        var locale = LocalizationSettings.AvailableLocales.GetLocale(targetCode);

        // Eğer bulamazsa index sırasına göre deniyoruz
        if (locale == null)
            locale = LocalizationSettings.AvailableLocales.Locales[_index];

        // Dili değiştiriyoruz
        LocalizationSettings.SelectedLocale = locale;

        yield return null;
        isChangingLanguage = false;

        Debug.Log("Dil değiştirildi: " + locale.Identifier.Code);
    }

    // =================================================
    //              DİĞER FONKSİYONLAR
    // =================================================
    public void OnDiscordClicked() { Application.OpenURL(discordLink); }
    public void OnYoutubeClicked() { Application.OpenURL(youtubeLink); }

    public void OnPlayClicked()
    {
        // 🔥 Kill sıfırla
        KillCounterUI.Instance?.ResetKills();

        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);

        gameWorldContainer.SetActive(true);

        if (menuCameraObj != null) menuCameraObj.SetActive(false);
        if (cmBrainCameraObj != null) cmBrainCameraObj.SetActive(true);

        // ✅ EKLENDİ: PLAY'e basınca player'ı belirlediğin noktalardan birine teleport et
        TeleportPlayerToRandomStartPoint();

        if (generateMapOnPlay && mapGeneratorScript != null)
            mapGeneratorScript.SendMessage("GenerateMap", SendMessageOptions.DontRequireReceiver);
    }

    // ✅ EKLENDİ
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

        // Null olmayan start noktalarını filtrele
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

        // Eğer NavMeshAgent varsa Warp daha temiz (havada yürüme/geri çekilme buglarını azaltır)
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

        // Normal teleport
        playerObj.transform.position = chosen.position;
        playerObj.transform.rotation = chosen.rotation;

        // Rigidbody varsa hızları sıfırla (teleport sonrası kayma olmasın)
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
        // 🔥 Leaderboard tekrar çek
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

        // 🎯 Steam Leaderboard çek
        if (SteamManager.Initialized && SteamLeaderboardManager.Instance != null)
        {
            SteamLeaderboardManager.Instance.DownloadTop10();
        }
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