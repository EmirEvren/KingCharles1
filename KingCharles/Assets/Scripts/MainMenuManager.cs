using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; 
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
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
    public string discordLink = "https://discord.gg/qhjW8etr";
    public string youtubeLink = "https://www.youtube.com/watch?v=DjaeJy_2lLk";

    [Header("--- DİL SİSTEMİ ---")]
    public Image languageFlagImage;      // Bayrağın değişeceği Image (UI)
    public TextMeshProUGUI languageNameText; // Dil isminin yazacağı Text (Varsa)
    
    // Dil listesi (Editörden doldurulacak)
    public List<LanguageData> languages; 
    
    // Hangi dilde olduğumuzu tutan sayı
    private int currentLanguageIndex = 0;
    
    // Kayıt anahtarı (Hata yapmamak için sabit değişken)
    private const string PREF_LANGUAGE_INDEX = "SelectedLanguageIndex";

    [System.Serializable]
    public struct LanguageData
    {
        public string languageName; // Örn: "English", "Türkçe"
        public Sprite flagSprite;   // Bayrak resmi
        public string languageCode; // "en", "tr"
    }

    private void Start()
    {
        // 1. ÖNCE DİL AYARLARINI YÜKLE (En kritik kısım burası)
        // Oyun açılır açılmaz hafızayı kontrol edip UI'ı günceller.
        LoadLanguageSettings();

        // 2. DİĞER BAŞLANGIÇ AYARLARI
        gameWorldContainer.SetActive(false);
        if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
        if(menuCameraObj != null) menuCameraObj.SetActive(true);
        
        ShowMainMenu();
    }

    // =================================================
    //              DİL SİSTEMİ (KAYITLI & OTO YÜKLEME)
    // =================================================

    // Oyun başlarken 1 kez çalışır
    private void LoadLanguageSettings()
    {
        // PlayerPrefs.GetInt("Key", 0) -> Eğer kayıt yoksa 0 (Varsayılan) döner.
        // Yani oyun ilk defa açılıyorsa otomatik olarak Element 0 (İngilizce) seçilir.
        // Eğer kayıt varsa (mesela 2 - Korece), o sayı gelir.
        currentLanguageIndex = PlayerPrefs.GetInt(PREF_LANGUAGE_INDEX, 0);

        // Güvenlik: Eğer kayıtlı sayı, listeden büyükse (listeyi değiştirirsen hata olmasın diye)
        if (languages.Count > 0 && currentLanguageIndex >= languages.Count) 
        {
            currentLanguageIndex = 0;
        }

        // UI'ı hemen güncelle ki oyuncu eski bayrağı görmesin
        UpdateLanguageUI();
    }

    // Dil butonuna tıklandığında çalışır
    public void OnLanguageToggleClicked()
    {
        if (languages.Count == 0) return;

        // Bir sonraki dile geç
        currentLanguageIndex++;

        // Listenin sonuna geldiysek başa dön (Döngü)
        if (currentLanguageIndex >= languages.Count)
        {
            currentLanguageIndex = 0;
        }

        // --- KAYIT İŞLEMİ ---
        // Yeni seçilen dili hafızaya atıyoruz.
        PlayerPrefs.SetInt(PREF_LANGUAGE_INDEX, currentLanguageIndex);
        PlayerPrefs.Save(); // Garanti olsun diye diske hemen yaz

        // Görünümü güncelle
        UpdateLanguageUI();
    }

    private void UpdateLanguageUI()
    {
        // Liste boşsa hata vermesin diye çık
        if (languages.Count == 0) return;

        // Mevcut dildeki verileri al
        LanguageData currentLang = languages[currentLanguageIndex];

        // 1. Bayrağı değiştir
        if (languageFlagImage != null)
            languageFlagImage.sprite = currentLang.flagSprite;

        // 2. Yazıyı değiştir (Varsa)
        if (languageNameText != null)
            languageNameText.text = currentLang.languageName;

        // Konsol kontrolü
        Debug.Log($"Dil Ayarlandı: {currentLang.languageName} (Kayıtlı Index: {currentLanguageIndex})");
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
        gameWorldContainer.SetActive(true);
        if(menuCameraObj != null) menuCameraObj.SetActive(false);
        if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(true);
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
            // DÜZELTME: Buradan 'gameWorldContainer.activeSelf' kontrolünü SİLDİK.
            // Artık oyun açıkken ESC'ye basarsan MainMenuManager karışmayacak.
            // İşi PauseManager halledecek.

            // Sadece Menüdeyken alt menüler (Ayarlar, Multiplayer) açıksa geri gelmesi için:
            if (settingsPanel.activeSelf || multiplayerPanel.activeSelf)
            {
                OnBackToMenuClicked();
            }
        }
    }
}