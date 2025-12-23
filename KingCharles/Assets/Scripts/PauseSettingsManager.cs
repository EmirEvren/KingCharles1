using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;            // EKLENDİ
using UnityEngine.Localization.Settings;   // EKLENDİ

public class PauseSettingsManager : MonoBehaviour
{
    // --- LOCALIZATION KEYS (Main Menu ile aynı) ---
    private const string KEY_SCREENMODE_EXCLUSIVE = "Key_ScreenMode_Exclusive";
    private const string KEY_SCREENMODE_BORDERLESS = "Key_ScreenMode_Borderless";
    private const string KEY_SCREENMODE_WINDOWED = "Key_ScreenMode_Windowed";

    [Header("--- MANAGER REFERANSI ---")]
    [Tooltip("Geri butonunun çalışması için sahnedeki PauseManager'ı buraya sürükle")]
    public PauseManager pauseManager;

    [Header("--- AUDIO ---")]
    public AudioMixer mainAudioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("--- GRAPHICS: RESOLUTION & SCREEN ---")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown; // EKLENDİ: Screen Mode Dropdown

    [Header("--- GRAPHICS: QUALITY & OTHERS ---")]
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;
    public Toggle hdrToggle;
    public Slider brightnessSlider;

    [Header("--- CONTROLS ---")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    private Resolution[] resolutions;

    // --- DİL DEĞİŞİMİNİ DİNLEME VE SETUP ---
    private void OnEnable()
    {
        // 1. Dil değişimi olayına abone ol
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;

        // 2. Dropdown içlerini doldur
        SetupResolutions();
        SetupScreenModes(); // EKLENDİ
        SetupQuality();

        // 3. Kayıtlı verileri çek ve UI'ı güncelle
        LoadSettings();
    }

    private void OnDisable()
    {
        // Script kapanınca dil dinlemeyi bırak
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Dil değiştiğinde otomatik çalışan fonksiyon
    private void OnLocaleChanged(Locale locale)
    {
        // Screen Mode listesini yeni dile göre güncelle
        SetupScreenModes();
    }

    private void Start()
    {
        AddListeners();
    }

    private void AddListeners()
    {
        // Grafik
        if (resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if (screenModeDropdown) screenModeDropdown.onValueChanged.AddListener(SetScreenMode); // EKLENDİ
        if (qualityDropdown) qualityDropdown.onValueChanged.AddListener(SetQuality);
        if (vsyncToggle) vsyncToggle.onValueChanged.AddListener(SetVSync);
        if (hdrToggle) hdrToggle.onValueChanged.AddListener(SetHDR);
        if (brightnessSlider) brightnessSlider.onValueChanged.AddListener(SetBrightness);

        // Ses
        if (musicSlider) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if (sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);

        // Kontrol
        if (sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
    }

    // =============================================================================
    //                      SETUP & LOAD (SENKRONİZASYON)
    // =============================================================================

    // --- YARDIMCI FONKSİYON: DİL ÇEVİRİSİ ---
    private string GetLocalizedText(string key)
    {
        // "MenuStringTableCollection" tablosu SettingsManager ile aynı olmalı
        var text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuStringTableCollection", key);

        if (string.IsNullOrEmpty(text))
        {
            return key;
        }
        return text;
    }

    private void SetupResolutions()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // --- SCREEN MODE SETUP (DİL DESTEKLİ) ---
    private void SetupScreenModes()
    {
        if (screenModeDropdown == null) return;

        // 1. Mevcut seçili ayarı hafızada tut
        int storedValue = screenModeDropdown.value;

        // 2. Listeyi temizle
        screenModeDropdown.ClearOptions();

        // 3. Yeni dildeki karşılıkları al
        List<string> options = new List<string>
        {
            GetLocalizedText(KEY_SCREENMODE_EXCLUSIVE),
            GetLocalizedText(KEY_SCREENMODE_BORDERLESS),
            GetLocalizedText(KEY_SCREENMODE_WINDOWED)
        };

        // 4. Listeyi tekrar doldur
        screenModeDropdown.AddOptions(options);

        // 5. Eski seçimi geri yükle
        screenModeDropdown.SetValueWithoutNotify(storedValue);
        screenModeDropdown.RefreshShownValue();
    }

    private void SetupQuality()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    // Menü her açıldığında PlayerPrefs'ten veriyi okur.
    public void LoadSettings()
    {
        // Screen Mode (EKLENDİ)
        int screenMode = PlayerPrefs.GetInt("ScreenMode", 1);
        if (screenModeDropdown) screenModeDropdown.SetValueWithoutNotify(screenMode);
        // Not: Pause menüde oyunu bozmamak için SetScreenMode'u burada çağırmıyoruz, 
        // zaten oyun başında SettingsManager bunu ayarlamıştı. Sadece UI'ı güncelliyoruz.

        // Quality
        int quality = PlayerPrefs.GetInt("QualityLevel", 2);
        if (qualityDropdown) qualityDropdown.SetValueWithoutNotify(quality);

        // VSync
        int vsync = PlayerPrefs.GetInt("VSync", 0);
        if (vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(vsync == 1);

        // HDR
        int hdr = PlayerPrefs.GetInt("HDR", 0);
        if (hdrToggle) hdrToggle.SetIsOnWithoutNotify(hdr == 1);

        // Audio
        if (musicSlider)
        {
            float musicVol = PlayerPrefs.GetFloat("MusicVol", 0.5f);
            musicSlider.SetValueWithoutNotify(musicVol);
        }
        if (sfxSlider)
        {
            float sfxVol = PlayerPrefs.GetFloat("SFXVol", 0.5f);
            sfxSlider.SetValueWithoutNotify(sfxVol);
        }

        // Sensitivity
        if (sensitivitySlider)
        {
            float sens = PlayerPrefs.GetFloat("Sensitivity", 100f);
            sensitivitySlider.SetValueWithoutNotify(sens);
            if (sensitivityValueText) sensitivityValueText.text = sens.ToString("0");
        }

        // Brightness
        if (brightnessSlider)
        {
            float bright = PlayerPrefs.GetFloat("Brightness", 1.0f);
            brightnessSlider.SetValueWithoutNotify(bright);
        }
    }

    // =============================================================================
    //                      AYAR FONKSİYONLARI (KAYIT EDER)
    // =============================================================================

    public void SetResolution(int resolutionIndex)
    {
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    // --- SCREEN MODE ACTION ---
    public void SetScreenMode(int modeIndex)
    {
        switch (modeIndex)
        {
            case 0: Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen; break;
            case 1: Screen.fullScreenMode = FullScreenMode.FullScreenWindow; break;
            case 2: Screen.fullScreenMode = FullScreenMode.Windowed; break;
        }
        PlayerPrefs.SetInt("ScreenMode", modeIndex);
    }

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);

        if (vsyncToggle) SetVSync(vsyncToggle.isOn);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    public void SetHDR(bool isEnabled)
    {
        PlayerPrefs.SetInt("HDR", isEnabled ? 1 : 0);
    }

    public void SetBrightness(float value)
    {
        RenderSettings.ambientIntensity = value;
        PlayerPrefs.SetFloat("Brightness", value);
    }

    public void SetMusicVolume(float volume)
    {
        if (volume <= 0.0001f) mainAudioMixer.SetFloat("MusicVolume", -80f);
        else mainAudioMixer.SetFloat("MusicVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("MusicVol", volume);
    }

    public void SetSFXVolume(float volume)
    {
        if (volume <= 0.0001f) mainAudioMixer.SetFloat("SFXVolume", -80f);
        else mainAudioMixer.SetFloat("SFXVolume", Mathf.Log10(volume) * 20);
        PlayerPrefs.SetFloat("SFXVol", volume);
    }

    public void SetSensitivity(float value)
    {
        // SettingsManager'daki statik değişkene ulaşıyoruz ki tüm oyun etkilensin
        SettingsManager.MouseSensitivity = value;

        if (sensitivityValueText != null) sensitivityValueText.text = value.ToString("0");
        PlayerPrefs.SetFloat("Sensitivity", value);
    }

    // =============================================================================
    //                  AÇMA / KAPAMA (NAVİGASYON)
    // =============================================================================

    public void CloseSettingsPanel()
    {
        if (pauseManager != null)
        {
            pauseManager.CloseSettings();
        }
        else
        {
            gameObject.SetActive(false);
            Debug.LogWarning("PauseManager atanmamış! Lütfen Inspector'dan atayın.");
        }
    }
}