using UnityEngine;
using UnityEngine.SceneManagement; // Restart için gerekli
using UnityEngine.InputSystem;     // Yeni Input System kullanıyorsun

public class PauseManager : MonoBehaviour
{
    [Header("--- UI REFERANSLARI ---")]
    [Tooltip("DURDURULDU yazan ana panel (Game World içindeki Canvas'ta olmalı)")]
    public GameObject pauseMenuPanel; 
    
    [Tooltip("Pause menüsü için oluşturduğun ÖZEL Ayarlar Paneli")]
    public GameObject pauseSettingsPanel; // Kopyaladığın Settings panelini buraya ata

    [Tooltip("Harita paneli (Varsa)")]
    public GameObject mapPanel;

    [Header("--- MANAGER REFERANSI ---")]
    [Tooltip("Ana menüye dönüşü yönetmek için Hiyerarşideki MainMenuManager'ı buraya sürükle")]
    public MainMenuManager mainMenuManager; 

    // Oyunun durup durmadığını kontrol eden değişken
    public static bool IsPaused = false;

    private void OnEnable()
    {
        // Game World aktif olduğunda (yani oyuna girdiğinde) her şeyi sıfırla
        IsPaused = false;
        Time.timeScale = 1f;

        if(pauseMenuPanel != null) pauseMenuPanel.SetActive(false);
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if(mapPanel != null) mapPanel.SetActive(false);
    }

    private void Update()
    {
        // EKSTRA GÜVENLİK: Eğer bu script bir şekilde GameWorld dışındaysa diye kontrol
        if (mainMenuManager != null && mainMenuManager.gameWorldContainer != null)
        {
            // Eğer Oyun Dünyası kapalıysa (Ana menüdeysek) ESC basılınca hiçbir şey yapma
            if (!mainMenuManager.gameWorldContainer.activeSelf) return;
        }

        // ESC Tuşuna basıldığında
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 1. Durum: Pause Ayarları Açıksa -> Kapat, Pause Menüye dön
            if (pauseSettingsPanel != null && pauseSettingsPanel.activeSelf)
            {
                CloseSettings();
            }
            // 2. Durum: Harita Açıksa -> Kapat, Pause Menüye dön
            else if (mapPanel != null && mapPanel.activeSelf)
            {
                CloseMap();
            }
            // 3. Durum: Hiçbiri açık değilse -> Oyunu Durdur veya Devam Ettir
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
    //              BUTON FONKSİYONLARI
    // =================================================

    // 1. YENİDEN DEVAM ET
    public void ResumeGame()
    {
        pauseMenuPanel.SetActive(false);
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        if(mapPanel != null) mapPanel.SetActive(false);

        Time.timeScale = 1f; // Zamanı akıt
        IsPaused = false;

        // Mouse imlecini gizle/kilitle
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Oyunu Durduran Fonksiyon
    private void PauseGame()
    {
        pauseMenuPanel.SetActive(true);
        Time.timeScale = 0f; // Zamanı durdur
        IsPaused = true;

        // Mouse imlecini serbest bırak
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // 2. HARİTA BUTONU
    public void OpenMap()
    {
        pauseMenuPanel.SetActive(false);
        if(mapPanel != null) mapPanel.SetActive(true);
    }

    public void CloseMap()
    {
        if(mapPanel != null) mapPanel.SetActive(false);
        pauseMenuPanel.SetActive(true); 
    }

    // 3. AYARLAR BUTONU
    public void OpenSettings()
    {
        pauseMenuPanel.SetActive(false); // Pause menüsü gizlensin
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(true); // Özel Pause Ayarları açılsın
    }

    public void CloseSettings()
    {
        if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
        pauseMenuPanel.SetActive(true); // Ayarlardan çıkınca Pause menüsü geri gelsin
    }

    // 4. YENİDEN BAŞLAT BUTONU
    public void RestartGame()
    {
        Time.timeScale = 1f;
        IsPaused = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // 5. MENÜYE DÖN BUTONU
    public void QuitToMainMenu()
    {
        // Zamanı normale döndür
        Time.timeScale = 1f;
        IsPaused = false;

        if (mainMenuManager != null)
        {
            // Panelleri kapat
            pauseMenuPanel.SetActive(false);
            if(pauseSettingsPanel != null) pauseSettingsPanel.SetActive(false);
            
            // Ana Menü yöneticisine işi devret
            mainMenuManager.OnBackToMenuClicked();
        }
        else
        {
            Debug.LogWarning("MainMenuManager atanmamış! Scriptin Inspector kısmından MainMenuManager'ı sürükle.");
        }
    }
}