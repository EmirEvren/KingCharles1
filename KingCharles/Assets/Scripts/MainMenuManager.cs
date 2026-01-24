using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; 
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Localization.Settings; 

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
            // Eğer Pause menüsünden Restart'a basıldıysa burası çalışır.
            RestartIstendi = false; // Değişkeni sıfırla ki bir sonraki normal açılışta menü gelsin.
            
            // Menüyü hiç göstermeden direkt oyunu başlat
            OnPlayClicked();
        }
        else
        {
            // Normal açılış (Oyun ilk açıldığında veya menüye dönüldüğünde)
            gameWorldContainer.SetActive(false);
            if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
            if(menuCameraObj != null) menuCameraObj.SetActive(true);
            
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
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        
        // Oyun dünyasını aktif et
        gameWorldContainer.SetActive(true);
        
        // Kameraları değiştir
        if(menuCameraObj != null) menuCameraObj.SetActive(false);
        if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(true);
        
        // Harita üretimi
        if (generateMapOnPlay && mapGeneratorScript != null)
            mapGeneratorScript.SendMessage("GenerateMap", SendMessageOptions.DontRequireReceiver);
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
        if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
        if(menuCameraObj != null) menuCameraObj.SetActive(true);
        mainMenuPanel.SetActive(true);
    }

    private void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
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