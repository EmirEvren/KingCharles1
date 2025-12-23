using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem; 
using TMPro; 
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Localization;            // EKLENDİ
using UnityEngine.Localization.Settings;   // EKLENDİ

public class SettingsManager : MonoBehaviour
{
    // --- LOCALIZATION KEYS ---
    private const string KEY_SCREENMODE_EXCLUSIVE = "Key_ScreenMode_Exclusive";
    private const string KEY_SCREENMODE_BORDERLESS = "Key_ScreenMode_Borderless";
    private const string KEY_SCREENMODE_WINDOWED = "Key_ScreenMode_Windowed";

    [Header("--- AUDIO ---")]
    public AudioMixer mainAudioMixer; 
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("--- GRAPHICS: RESOLUTION & SCREEN ---")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown screenModeDropdown; 
    private Resolution[] resolutions;

    [Header("--- GRAPHICS: QUALITY & POST-PROCESSING ---")]
    public TMP_Dropdown qualityDropdown; 
    public TMP_Dropdown aaDropdown;      
    public Toggle vsyncToggle;
    public Toggle hdrToggle;
    public Slider brightnessSlider;      

    [Header("--- CONTROLS ---")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    public static float MouseSensitivity = 100f; 

    // --- DİL DEĞİŞİMİNİ DİNLEMEK İÇİN GEREKLİ ---
    private void OnEnable()
    {
        // Dil değişirse "OnLocaleChanged" fonksiyonunu çalıştır
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnDisable()
    {
        // Script kapanınca dinlemeyi bırak (Hata vermemesi için)
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    // Dil değiştiğinde otomatik çalışan fonksiyon
    private void OnLocaleChanged(Locale locale)
    {
        // Sadece Screen Mode dropdown'ını güncellememiz yeterli, 
        // çünkü içindeki metinler dinamik. Diğerleri (Quality vs) Unity'den geliyor.
        SetupScreenModes();
    }

    private void Start()
    {
        Application.targetFrameRate = -1;

        SetupResolutions();  
        SetupScreenModes();  
        SetupQuality();      
        SetupAA();           

        LoadSettings();
        AddListeners();
    }

    private void AddListeners()
    {
        resolutionDropdown.onValueChanged.AddListener(SetResolution);
        screenModeDropdown.onValueChanged.AddListener(SetScreenMode);
        qualityDropdown.onValueChanged.AddListener(SetQuality);
        aaDropdown.onValueChanged.AddListener(SetAntiAliasing);
        vsyncToggle.onValueChanged.AddListener(SetVSync);
        hdrToggle.onValueChanged.AddListener(SetHDR);
        brightnessSlider.onValueChanged.AddListener(SetBrightness);

        musicSlider.onValueChanged.AddListener(SetMusicVolume);
        sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
    }

    // =============================================================================
    //                               SETUP FUNCTIONS
    // =============================================================================

    private string GetLocalizedText(string key)
    {
        // "MenuStringTableCollection" tablosundan veriyi çek
        var text = LocalizationSettings.StringDatabase.GetLocalizedString("MenuStringTableCollection", key);
        
        if (string.IsNullOrEmpty(text))
        {
            return key; 
        }
        return text; 
    }

    private void SetupResolutions()
    {
        resolutions = Screen.resolutions.Select(resolution => new Resolution { width = resolution.width, height = resolution.height }).Distinct().ToArray();
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height;
            options.Add(option);
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
                currentResolutionIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        resolutionDropdown.RefreshShownValue();
    }

    // --- GÜNCELLENEN KISIM: DİL DEĞİŞİNCE BOZULMAMASI İÇİN ---
    private void SetupScreenModes()
    {
        // 1. Mevcut seçili ayarı (0, 1 veya 2) hafızada tut
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

        // 5. Eski seçimi geri yükle (yoksa liste sıfırlanır ve en başa döner)
        screenModeDropdown.SetValueWithoutNotify(storedValue);
        screenModeDropdown.RefreshShownValue();
    }

    private void SetupQuality()
    {
        qualityDropdown.ClearOptions();
        List<string> options = new List<string>(QualitySettings.names);
        qualityDropdown.AddOptions(options);
    }

    private void SetupAA()
    {
        aaDropdown.ClearOptions();
        List<string> options = new List<string> { "Off", "2x MSAA", "4x MSAA", "8x MSAA" };
        aaDropdown.AddOptions(options);
    }

    // =============================================================================
    //                               GRAPHICS LOGIC
    // =============================================================================

    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutions.Length) return;
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

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
        
        if (vsyncToggle != null) SetVSync(vsyncToggle.isOn);
        if (aaDropdown != null) SetAntiAliasing(aaDropdown.value);
    }

    public void SetAntiAliasing(int aaIndex)
    {
        switch (aaIndex)
        {
            case 0: QualitySettings.antiAliasing = 0; break;
            case 1: QualitySettings.antiAliasing = 2; break;
            case 2: QualitySettings.antiAliasing = 4; break;
            case 3: QualitySettings.antiAliasing = 8; break;
        }
        PlayerPrefs.SetInt("AA_Level", aaIndex);
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

    // =============================================================================
    //                               AUDIO LOGIC
    // =============================================================================

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

    // =============================================================================
    //                               CONTROLS LOGIC
    // =============================================================================

    public void SetSensitivity(float value)
    {
        MouseSensitivity = value;
        if (sensitivityValueText != null) sensitivityValueText.text = value.ToString("0");
        PlayerPrefs.SetFloat("Sensitivity", value);
    }

    // =============================================================================
    //                               LOAD SETTINGS
    // =============================================================================

    private void LoadSettings()
    {
        // Screen Mode
        int screenMode = PlayerPrefs.GetInt("ScreenMode", 1);
        // Önce dropdown değerini ayarla, sonra Switch ile uygula
        if(screenModeDropdown != null) screenModeDropdown.SetValueWithoutNotify(screenMode);
        SetScreenMode(screenMode);

        // Quality
        int quality = PlayerPrefs.GetInt("QualityLevel", 2);
        if (qualityDropdown != null) qualityDropdown.value = quality;
        QualitySettings.SetQualityLevel(quality);

        // AA 
        int aa = PlayerPrefs.GetInt("AA_Level", 2);
        if (aaDropdown != null) aaDropdown.value = aa;
        SetAntiAliasing(aa);
        
        // VSync
        int vsync = PlayerPrefs.GetInt("VSync", 0);
        if(vsyncToggle != null) vsyncToggle.isOn = (vsync == 1);
        SetVSync(vsync == 1);

        // HDR
        int hdr = PlayerPrefs.GetInt("HDR", 0);
        if(hdrToggle != null) hdrToggle.isOn = (hdr == 1);

        // Audio
        if (musicSlider != null) 
        {
            float savedVol = PlayerPrefs.GetFloat("MusicVol", 0.5f);
            musicSlider.value = savedVol;
            SetMusicVolume(savedVol);
        }
        if (sfxSlider != null) 
        {
            float savedVol = PlayerPrefs.GetFloat("SFXVol", 0.5f);
            sfxSlider.value = savedVol;
            SetSFXVolume(savedVol);
        }

        // Sensitivity
        float sens = PlayerPrefs.GetFloat("Sensitivity", 100f);
        if (sensitivitySlider != null) 
        {
            sensitivitySlider.value = sens;
            SetSensitivity(sens);
        }
        
        // Brightness
        float bright = PlayerPrefs.GetFloat("Brightness", 1.0f);
        if(brightnessSlider != null)
        {
            brightnessSlider.value = bright;
            SetBrightness(bright);
        }
    }
}