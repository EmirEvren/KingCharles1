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

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);
    }

    private void Update()
    {
        // 1. GÜVENLİK KONTROLÜ
        if (mainMenuManager != null && mainMenuManager.gameWorldContainer != null)
        {
            // Eğer Ana Menüdeysek (Oyun dünyası kapalıysa) bu script çalışmasın
            if (!mainMenuManager.gameWorldContainer.activeSelf) return;
        }

        // 1.5 DEATH SCREEN KONTROLÜ (ÖNEMLİ)
        if (IsDeathScreenOpen())
        {
            // DeathScreen açıkken pause UI'ları kapalı kalsın + input ignore
            ForceClosePauseUI();
            return;
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
    //              DEATHSCREEN GUARD
    // =================================================

    private bool IsDeathScreenOpen()
    {
        return DeathScreenUI.Instance != null && DeathScreenUI.Instance.IsOpen;
    }

    private void ForceClosePauseUI()
    {
        if (pauseMenuPanel != null && pauseMenuPanel.activeSelf) pauseMenuPanel.SetActive(false);
        if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf) pauseSettingsPanel.SetActive(false);
        if (mapPanel != null && mapPanel.activeSelf) mapPanel.SetActive(false);

        // Pause state’i de temizle (ResumeGame tetiklenmesin)
        IsPaused = false;
    }

    // =================================================
    //              TEMEL FONKSİYONLAR
    // =================================================

    public void ResumeGame()
    {
        if (IsDeathScreenOpen()) return;

        // Her şeyi kapat
        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        // Settings zaten pauseMenuPanel içinde olduğu için o da kapanır ama garanti olsun:
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if (mapPanel != null) mapPanel.SetActive(false);

        Time.timeScale = 1f; // Zamanı akıt
        IsPaused = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void PauseGame()
    {
        if (IsDeathScreenOpen()) return;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(true);

        // Pause ilk açıldığında ana içerikler GÖRÜNÜR, ayarlar GİZLİ olmalı
        ToggleMainContent(true);
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

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
        if (IsDeathScreenOpen()) return;

        if (pauseMenuPanel != null) pauseMenuPanel.SetActive(false); // Harita tam ekran olacağı için menüyü kapat
        if (mapPanel != null) mapPanel.SetActive(true);
    }

    public void CloseMap()
    {
        if (IsDeathScreenOpen()) return;

        if (mapPanel != null) mapPanel.SetActive(false);
        PauseGame(); // Harita kapanınca oyun devam etmez, Pause menüsüne döner
    }

    // =================================================
    //              AYARLAR İŞLEMLERİ (ÖNEMLİ)
    // =================================================

    public void OpenSettings()
    {
        if (IsDeathScreenOpen()) return;

        // DİKKAT: pauseMenuPanel'i kapatmıyoruz! Çünkü Ayarlar onun içinde.
        // Sadece içindeki diğer kalabalık yapan şeyleri gizliyoruz.
        ToggleMainContent(false);

        // Ayarlar panelini aç
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        if (IsDeathScreenOpen()) return;

        // Ayarlar panelini kapat
        if (pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);

        // Ana içerikleri (Envanter, Butonlar) geri getir
        ToggleMainContent(true);
    }

    // Resimdeki objeleri topluca açıp kapatan yardımcı fonksiyon
    private void ToggleMainContent(bool isActive)
    {
        if (leftInventoryPanel) leftInventoryPanel.SetActive(isActive);
        if (rightStatsPanel) rightStatsPanel.SetActive(isActive);
        if (buttonsGroup) buttonsGroup.SetActive(isActive);
        if (titleText) titleText.SetActive(isActive);
    }

    // =================================================
    //              DİĞER BUTONLAR
    // =================================================

    public void RestartGame()
    {
        // DeathScreen açıkken de restart edebilmek istersen bu guard'ı kaldırabilirsin.
        // Şimdilik güvenli olması için kapatmıyorum:
        // if (IsDeathScreenOpen()) return;

        // 1. Önce zamanı ve pause durumunu düzelt
        Time.timeScale = 1f;
        IsPaused = false;

        // 2. KRİTİK KISIM: MainMenuManager'a "Bu bir restarttır, menüyü açma" diyoruz.
        MainMenuManager.RestartIstendi = true;

        // 3. Sahneyi yeniden yükle
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMainMenu()
    {
        // DeathScreen açıkken de ana menüye çıkabilmek istersen bu guard'ı kaldırabilirsin.
        // Şimdilik güvenli olması için kapatmıyorum:
        // if (IsDeathScreenOpen()) return;

        // 1. Zamanı normale döndür (Yoksa menüde animasyonlar çalışmaz)
        Time.timeScale = 1f;
        IsPaused = false;

        // 2. ÖNEMLİ: Restart bayrağını FALSE yapıyoruz.
        // Böylece sahne yüklendiğinde MainMenuManager "Ha, restart istenmemiş, menüyü açayım" der.
        MainMenuManager.RestartIstendi = false;

        // 3. Sahneyi baştan yükle
        // Bu işlem haritayı, karakteri, envanteri siler ve her şeyi "Start" haline getirir.
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
