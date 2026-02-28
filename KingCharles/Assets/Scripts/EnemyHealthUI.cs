using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthUI : MonoBehaviour
{
    [Header("Referanslar")]
    public EnemyHealth enemyHealth; // Can verisini çekeceğimiz asıl script
    public Slider healthSlider;     // Güncelleyeceğimiz Slider UI

    [Header("Billboard Ayarları")]
    public bool lookAtCamera = true; // Her zaman kameraya baksın mı?

    private Camera mainCamera;

    private void Start()
    {
        // Eğer atanmamışsa, aynı objedeki EnemyHealth'i bul
        if (enemyHealth == null)
        {
            enemyHealth = GetComponentInParent<EnemyHealth>();
        }

        // Ana kamerayı bul
        mainCamera = Camera.main;

        // Slider'ın max değerini başta 1 yapalım (Yüzdelik çalışacağız)
        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            UpdateHealthBar(); // Başlangıçta canı ful göster
        }
    }

    private void LateUpdate()
    {
        // 1. Can Barını Güncelle
        UpdateHealthBar();

        // 2. Kameraya Doğru Dön (Billboard Etkisi)
        if (lookAtCamera && mainCamera != null)
        {
            // Canvas kameraya baksın ama ters dönmesin diye
            transform.LookAt(transform.position + mainCamera.transform.rotation * Vector3.forward,
                             mainCamera.transform.rotation * Vector3.up);
        }
    }

    private void UpdateHealthBar()
    {
        if (enemyHealth != null && healthSlider != null)
        {
            // Güncel canın, maksimum cana oranını (0 ile 1 arası bir değer) bul
            float currentHealth = enemyHealth.GetCurrentHealth();
            float maxHealth = enemyHealth.GetMaxHealth();
            
            // Eğer max can 0 ise bölme hatası almamak için kontrol ekleyelim
            if (maxHealth > 0)
            {
                healthSlider.value = currentHealth / maxHealth;
            }
        }
    }
}