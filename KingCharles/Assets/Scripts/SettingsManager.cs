using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.InputSystem; 
using TMPro; 
using System.Collections.Generic;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
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

    private void Start()
    {
        // ÖNEMLİ: VSync'in kontrolü ele alabilmesi için Unity'nin kendi FPS limitini kaldırıyoruz.
        Application.targetFrameRate = -1;

        // 1. TÜM DROPDOWN'LARI DOLDUR
        SetupResolutions();  
        SetupScreenModes();  
        SetupQuality();      
        SetupAA();           

        // 2. KAYITLI AYARLARI YÜKLE
        LoadSettings();
        
        // 3. LISTENER'LARI EKLE
        // Listener'ları en son ekliyoruz ki LoadSettings sırasında gereksiz tetiklenmesinler.
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

    private void SetupScreenModes()
    {
        screenModeDropdown.ClearOptions();
        List<string> options = new List<string> { "Exclusive Fullscreen", "Borderless Window", "Windowed" };
        screenModeDropdown.AddOptions(options);
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

    // --- KRİTİK DÜZELTME BURADA ---
    public void SetQuality(int qualityIndex)
    {
        // 1. Kalite seviyesini değiştir
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);

        // 2. DÜZELTME: Unity, kalite seviyesi değişince VSync ve AA ayarlarını 
        // o kalitenin varsayılanına sıfırlar. Biz bunu engellemek için
        // UI'daki seçili ayarları ZORLA tekrar uyguluyoruz.
        
        if (vsyncToggle != null) SetVSync(vsyncToggle.isOn);
        if (aaDropdown != null) SetAntiAliasing(aaDropdown.value);
    }

    public void SetAntiAliasing(int aaIndex)
    {
        // Not: Eğer URP kullanıyorsan bu kod çalışmaz, URP Asset üzerinden ayar gerekir.
        // Built-in Render Pipeline için bu kod doğrudur.
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
        // vSyncCount: 0 = Kapalı, 1 = Açık (Monitör Hz'ine kilitler)
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    public void SetHDR(bool isEnabled)
    {
        // HDR genellikle RenderPipelineAsset üzerinden yönetilir ama 
        // Built-in'de bazı durumlarda bu yeterlidir.
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
        if(screenModeDropdown != null) screenModeDropdown.value = screenMode;
        SetScreenMode(screenMode);

        // Quality
        int quality = PlayerPrefs.GetInt("QualityLevel", 2);
        if (qualityDropdown != null) qualityDropdown.value = quality;
        // Burada SetQuality çağırmıyoruz çünkü SetQuality içinde VSync/AA tekrar tetikleniyor.
        // Sadece Unity ayarını yapıyoruz:
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