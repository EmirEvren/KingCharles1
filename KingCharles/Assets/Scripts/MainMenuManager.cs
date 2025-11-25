using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro; // TextMeshPro için gerekli (Dil ismi yazısı için)
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
    public Image languageFlagImage;      // Bayrağın olduğu Image
    public TextMeshProUGUI languageNameText; // Bayrağın altındaki yazı (TMP)
    public List<LanguageData> languages; // Editörden dolduracağın dil listesi
    private int currentLanguageIndex = 0;

    // Dil verisi tutacak yapı
    [System.Serializable]
    public struct LanguageData
    {
        public string languageName; // Örn: "Türkçe", "English"
        public Sprite flagSprite;   // O ülkenin bayrağı
        public string languageCode; // Örn: "tr", "en" (İlerde Localization sistemi için lazım olur)
    }

    private void Start()
    {
        gameWorldContainer.SetActive(false);
        if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(false);
        if(menuCameraObj != null) menuCameraObj.SetActive(true);
        
        // Başlangıç dilini ayarla (Kaydedilmiş bir dil varsa onu çekebilirsin, şimdilik 0)
        UpdateLanguageUI();

        ShowMainMenu();
    }

    // =================================================
    //              SOSYAL MEDYA BUTONLARI
    // =================================================

    public void OnDiscordClicked()
    {
        Debug.Log("💬 Discord açılıyor...");
        Application.OpenURL(discordLink);
    }

    public void OnYoutubeClicked()
    {
        Debug.Log("📺 YouTube açılıyor...");
        Application.OpenURL(youtubeLink);
    }

    // =================================================
    //              DİL DEĞİŞTİRME BUTONU
    // =================================================

    public void OnLanguageToggleClicked()
    {
        // Bir sonraki dile geç
        currentLanguageIndex++;

        // Eğer listenin sonuna geldiysek başa dön (Döngü)
        if (currentLanguageIndex >= languages.Count)
        {
            currentLanguageIndex = 0;
        }

        UpdateLanguageUI();
    }

    private void UpdateLanguageUI()
    {
        if (languages.Count == 0) return;

        LanguageData currentLang = languages[currentLanguageIndex];

        // 1. Bayrağı değiştir
        if (languageFlagImage != null)
            languageFlagImage.sprite = currentLang.flagSprite;

        // 2. Yazıyı değiştir (O dildeki ismi)
        if (languageNameText != null)
            languageNameText.text = currentLang.languageName;

        // 3. (Opsiyonel) Gerçek Oyun Dilini Değiştirme Kodu
        // Örnek: LocalizationSettings.SelectedLocale = ...
        Debug.Log($"Dil Değişti: {currentLang.languageName} ({currentLang.languageCode})");
    }

    // =================================================
    //              MEVCUT BUTONLAR
    // =================================================

    public void OnPlayClicked()
    {
        Debug.Log("🐶 Alpha Mode: OYUN BAŞLIYOR!");
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(false);
        multiplayerPanel.SetActive(false);
        gameWorldContainer.SetActive(true);
        if(menuCameraObj != null) menuCameraObj.SetActive(false);
        if(cmBrainCameraObj != null) cmBrainCameraObj.SetActive(true);
        if (generateMapOnPlay && mapGeneratorScript != null)
        {
            mapGeneratorScript.SendMessage("GenerateMap", SendMessageOptions.DontRequireReceiver);
        }
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

    public void OnExitClicked()
    {
        Application.Quit();
    }

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
            if (gameWorldContainer.activeSelf || settingsPanel.activeSelf || multiplayerPanel.activeSelf)
            {
                OnBackToMenuClicked();
            }
        }
    }
}