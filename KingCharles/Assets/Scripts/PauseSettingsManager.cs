using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class PauseSettingsManager : MonoBehaviour
{
    [Header("--- MANAGER REFERANSI ---")]
    [Tooltip("Geri butonunun çalışması için sahnedeki PauseManager'ı buraya sürükle")]
    public PauseManager pauseManager;

    [Header("--- AUDIO ---")]
    public AudioMixer mainAudioMixer;
    public Slider musicSlider;
    public Slider sfxSlider;

    [Header("--- GRAPHICS ---")]
    public TMP_Dropdown resolutionDropdown; 
    public TMP_Dropdown qualityDropdown;
    public Toggle vsyncToggle;
    public Toggle hdrToggle; // EKLENDİ: HDR Toggle
    public Slider brightnessSlider;

    [Header("--- CONTROLS ---")]
    public Slider sensitivitySlider;
    public TextMeshProUGUI sensitivityValueText;

    private Resolution[] resolutions;

    // Script aktif olduğunda (ESC'ye basıp paneli açtığında) çalışır.
    // SENKRONİZASYON BURADA SAĞLANIR.
    private void OnEnable()
    {
        // Önce dropdown içlerini doldur
        SetupResolutions();
        SetupQuality();

        // Sonra kayıtlı verileri çek ve UI'ı güncelle
        LoadSettings();
    }

    private void Start()
    {
        // Listener'ları sadece bir kez tanımlıyoruz
        AddListeners();
    }

    private void AddListeners()
    {
        // Grafik
        if(resolutionDropdown) resolutionDropdown.onValueChanged.AddListener(SetResolution);
        if(qualityDropdown) qualityDropdown.onValueChanged.AddListener(SetQuality);
        if(vsyncToggle) vsyncToggle.onValueChanged.AddListener(SetVSync);
        if(hdrToggle) hdrToggle.onValueChanged.AddListener(SetHDR); // EKLENDİ
        if(brightnessSlider) brightnessSlider.onValueChanged.AddListener(SetBrightness);
        
        // Ses
        if(musicSlider) musicSlider.onValueChanged.AddListener(SetMusicVolume);
        if(sfxSlider) sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        
        // Kontrol
        if(sensitivitySlider) sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
    }

    // =============================================================================
    //                      SETUP & LOAD (SENKRONİZASYON)
    // =============================================================================

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

    private void SetupQuality()
    {
        if (qualityDropdown == null) return;
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new List<string>(QualitySettings.names));
    }

    // Menü her açıldığında PlayerPrefs'ten veriyi okur.
    public void LoadSettings()
    {
        // Quality
        int quality = PlayerPrefs.GetInt("QualityLevel", 2);
        if(qualityDropdown) qualityDropdown.SetValueWithoutNotify(quality);

        // VSync
        int vsync = PlayerPrefs.GetInt("VSync", 0);
        if(vsyncToggle) vsyncToggle.SetIsOnWithoutNotify(vsync == 1);

        // HDR (EKLENDİ)
        int hdr = PlayerPrefs.GetInt("HDR", 0);
        if(hdrToggle) hdrToggle.SetIsOnWithoutNotify(hdr == 1);

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
            if(sensitivityValueText) sensitivityValueText.text = sens.ToString("0");
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

    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityLevel", qualityIndex);
        
        // Quality değişince VSync bozulabilir, tekrar uygula
        if(vsyncToggle) SetVSync(vsyncToggle.isOn);
    }

    public void SetVSync(bool isEnabled)
    {
        QualitySettings.vSyncCount = isEnabled ? 1 : 0;
        PlayerPrefs.SetInt("VSync", isEnabled ? 1 : 0);
    }

    // EKLENDİ: HDR FONKSİYONU
    public void SetHDR(bool isEnabled)
    {
        // Built-in render pipeline kullanıyorsan PlayerPrefs kaydı yeterlidir,
        // URP kullanıyorsan RenderPipelineAsset üzerinden erişim gerekir.
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
        // Statik değişkeni güncelle
        SettingsManager.MouseSensitivity = value; 
        
        if (sensitivityValueText != null) sensitivityValueText.text = value.ToString("0");
        PlayerPrefs.SetFloat("Sensitivity", value);
    }

    // =============================================================================
    //                  AÇMA / KAPAMA (NAVİGASYON)
    // =============================================================================

    // Bu fonksiyonu "Geri" (Back) butonuna ata
    public void CloseSettingsPanel()
    {
        if (pauseManager != null)
        {
            // PauseManager üzerinden kapat ki Inventory/Stats geri gelsin
            pauseManager.CloseSettings();
        }
        else
        {
            // Eğer PauseManager atanmadıysa en azından paneli kapat
            gameObject.SetActive(false);
            Debug.LogWarning("PauseManager atanmamış! Lütfen Inspector'dan atayın.");
        }
    }
}