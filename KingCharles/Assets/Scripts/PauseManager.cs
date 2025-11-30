using UnityEngine;
using UnityEngine.SceneManagement; 
using UnityEngine.InputSystem;     

public class PauseManager : MonoBehaviour
{
    [Header("--- UI ANA KAPSIYICILAR ---")]
    [Tooltip("Tüm her şeyi kapsayan ana panel (PauseMenuPanel)")]
    public GameObject pauseMenuPanel; 
    
    [Tooltip("Pause menüsü içine oluşturduğun Settings_Inner_Panel")]
    public GameObject pauseSettingsPanel; 

    [Tooltip("Harita paneli")]
    public GameObject mapPanel;

    [Header("--- GİZLENECEK NESNELER (RESİMDEKİLER) ---")]
    // Ayarlar açılınca bunların gizlenmesi lazım, yoksa üst üste binerler.
    public GameObject leftInventoryPanel; // Resimdeki: Left_InventoryPanel
    public GameObject rightStatsPanel;    // Resimdeki: Right_StatsPanel
    public GameObject buttonsGroup;       // Resimdeki: Buttons_Group
    public GameObject titleText;          // Resimdeki: TitleText (İstersen kalsın, istersen gizle)

    [Header("--- MANAGER REFERANSI ---")]
    public MainMenuManager mainMenuManager; 

    // Oyunun durup durmadığını kontrol eden değişken
    public static bool IsPaused = false;

    private void OnEnable()
    {
        // Oyun başladığında veya obje açıldığında sıfırlama
        IsPaused = false;
        Time.timeScale = 1f;

        if(pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if(mapPanel != null) mapPanel.SetActive(false);
    }

    private void Update()
    {
        // 1. GÜVENLİK KONTROLÜ
        if (mainMenuManager != null && mainMenuManager.gameWorldContainer != null)
        {
            // Eğer Ana Menüdeysek (Oyun dünyası kapalıysa) bu script çalışmasın
            if (!mainMenuManager.gameWorldContainer.activeSelf) return;
        }

        // 2. ESC TUŞU KONTROLÜ
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // A) Ayarlar açıksa -> Ayarları kapat, Menüye dön
            if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
            {
                CloseSettings();
            }
            // B) Harita açıksa -> Haritayı kapat, Menüye dön
            else if (mapPanel != null && mapPanel.activeSelf)
            {
                CloseMap();
            }
            // C) Hiçbiri açık değilse -> Oyunu Durdur/Başlat
            else
            {
                if (IsPaused)
                    ResumeGame();
                else
                    PauseGame();
            }
        }
    }

    // =================================================
    //              TEMEL FONKSİYONLAR
    // =================================================

    public void ResumeGame()
    {
        // Her şeyi kapat
        pauseMenuPanel.SetActive(false);
        // Settings zaten pauseMenuPanel içinde olduğu için o da kapanır ama garanti olsun:
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if(mapPanel != null) mapPanel.SetActive(false);

        Time.timeScale = 1f; // Zamanı akıt
        IsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        
        // Pause ilk açıldığında ana içerikler GÖRÜNÜR, ayarlar GİZLİ olmalı
        ToggleMainContent(true);
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

        Time.timeScale = 0f; // Zamanı durdur
        IsPaused = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // =================================================
    //              HARİTA İŞLEMLERİ
    // =================================================

    public void OpenMap()
    {
        pauseMenuPanel.SetActive(false); // Harita tam ekran olacağı için menüyü kapat
        if(mapPanel != null) mapPanel.SetActive(true);
    }

    public void CloseMap()
    {
        if(mapPanel != null) mapPanel.SetActive(false);
        PauseGame(); // Harita kapanınca oyun devam etmez, Pause menüsüne döner
    }

    // =================================================
    //              AYARLAR İŞLEMLERİ (ÖNEMLİ)
    // =================================================

    public void OpenSettings()
    {
        // DİKKAT: pauseMenuPanel'i kapatmıyoruz! Çünkü Ayarlar onun içinde.
        // Sadece içindeki diğer kalabalık yapan şeyleri gizliyoruz.
        ToggleMainContent(false);

        // Ayarlar panelini aç
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        // Ayarlar panelini kapat
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

        // Ana içerikleri (Envanter, Butonlar) geri getir
        ToggleMainContent(true);
    }

    // Resimdeki objeleri topluca açıp kapatan yardımcı fonksiyon
    private void ToggleMainContent(bool isActive)
    {
        if(leftInventoryPanel) leftInventoryPanel.SetActive(isActive);
        if(rightStatsPanel) rightStatsPanel.SetActive(isActive);
        if(buttonsGroup) buttonsGroup.SetActive(isActive);
        if(titleText) titleText.SetActive(isActive);
    }

    // =================================================
    //              DİĞER BUTONLAR
    // =================================================

    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        IsPaused = false;

        if (mainMenuManager != null)
        {
            pauseMenuPanel.SetActive(false);
            mainMenuManager.OnBackToMenuClicked();
        }
        else
        {
            // Fallback: Eğer manager yoksa direkt sahneyi yeniden yükle veya çık
            Debug.LogWarning("MainMenuManager atanmamış!");
            SceneManager.LoadScene(0); // Genelde 0. sahne menüdür
        }
    }
}