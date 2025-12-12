using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Ayarları")]
    public float maxHealth = 50f;      // Inspector'dan değiştirebilirsin, default 50
    [SerializeField] private float currentHealth;

    [Header("Ölüm Ayarları")]
    public float destroyDelay = 2f;    // Ölüm animasyonu süresi / düşmanın sahnede kalma süresi

    [Header("Opsiyonel Animasyon")]
    public Animator animator;          // Ölüm animasyonu için istersen bağla
    public string deathTriggerName = "Death"; // Animator'daki Death trigger ismi

    private bool isDead = false;

    [Header("Vurulma Efekti (Hit Flash)")]
    public Renderer[] renderersToFlash;    // Boş bırakırsan otomatik bulur
    public Color hitColor = Color.red;     // Vurulduğunda olacak renk
    public float flashDuration = 0.1f;     // Ne kadar süre kırmızı kalsın

    private Material[] _materials;
    private Color[] _originalColors;
    private bool _isFlashing = false;

    [Header("XP Drop")]
    public GameObject xpPrefab;       // Inspector’dan atacağın XP kutusu prefabı
    public int xpBoxMin = 1;          // Min kutu sayısı
    public int xpBoxMax = 3;          // Max kutu sayısı
    public float xpSpawnRadius = 0.5f; // Etrafına ne kadar saçılacak

    [Header("Gold Drop (Sadece Elite)")]
    public GameObject goldPrefab;         // Altın parçası prefabı
    public int goldMinPieces = 1;         // Elite başına minimum altın parçası
    public int goldMaxPieces = 5;         // Elite başına maksimum altın parçası (istenen: 5)
    public float goldSpawnRadius = 0.5f;  // Altınların etrafa saçılma yarıçapı

    private void Awake()
    {
        // Başlangıçta canı maksimuma eşitle
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();

        // Renderer'ları ayarla
        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            // Düşmanın çocuklarındaki tüm Renderer'ları otomatik bul
            renderersToFlash = GetComponentsInChildren<Renderer>();
        }

        // Materyal ve orijinal renkleri cache'le
        _materials = new Material[renderersToFlash.Length];
        _originalColors = new Color[renderersToFlash.Length];

        for (int i = 0; i < renderersToFlash.Length; i++)
        {
            // Her enemy için ayrı material instance alıyoruz
            _materials[i] = renderersToFlash[i].material;

            // Hem Built-in hem URP için dene
            if (_materials[i].HasProperty("_BaseColor"))
                _originalColors[i] = _materials[i].GetColor("_BaseColor");
            else if (_materials[i].HasProperty("_Color"))
                _originalColors[i] = _materials[i].GetColor("_Color");
            else
                _originalColors[i] = Color.white; // fallback
        }
    }

    /// <summary>
    /// Bu fonksiyon dışardan çağrılacak. (Oyuncu vurduğunda vs.)
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // --- DAMAGE POPUP ---
        DamagePopupManager.Show(
            amount,
            transform.position,   // düşmanın pozisyonu
            Color.red             // istersen crit vs. için renk değiştiririz
        );

        // Her damage aldığında kırmızı flash yapsın
        StartHitFlash();

        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Ölüm animasyonu tetikle
        if (animator != null && !string.IsNullOrEmpty(deathTriggerName))
        {
            animator.SetTrigger(deathTriggerName);
        }

        // XP ve Destroy'u geciktir → ölüm animasyonu bittikten sonra olsun
        StartCoroutine(DeathRoutine());
    }

    private System.Collections.IEnumerator DeathRoutine()
    {
        // Ölüm animasyonu süresi kadar bekle
        if (destroyDelay > 0f)
            yield return new WaitForSeconds(destroyDelay);

        // Tam yok olurken XP kutularını spawn et
        SpawnXPBoxes();

        // Elite mi? maxHealth >= 150 ise ELITE kabul ediyoruz
        bool isEliteKill = maxHealth >= 150f;

        // Elite ise altın parçalarını spawn et
        if (isEliteKill)
        {
            SpawnGoldPieces();
        }

        // --- KILL COUNTER ---
        // Elite: +5, normal: +1
        int addKills = isEliteKill ? 5 : 1;
        for (int i = 0; i < addKills; i++)
        {
            KillCounterUI.RegisterKill();
        }

        // Artık düşmanı yok et
        Destroy(gameObject);
    }

    // İstersen başka scriptlerden okunabilsin
    public float GetCurrentHealth() => currentHealth;
    public float GetMaxHealth() => maxHealth;

    #region Hit Flash

    private void StartHitFlash()
    {
        if (!_isFlashing && gameObject.activeInHierarchy)
        {
            StartCoroutine(HitFlashRoutine());
        }
    }

    private System.Collections.IEnumerator HitFlashRoutine()
    {
        _isFlashing = true;

        // Kırmızı yap
        for (int i = 0; i < _materials.Length; i++)
        {
            if (_materials[i] == null) continue;

            if (_materials[i].HasProperty("_BaseColor"))
                _materials[i].SetColor("_BaseColor", hitColor);
            else if (_materials[i].HasProperty("_Color"))
                _materials[i].SetColor("_Color", hitColor);
        }

        yield return new WaitForSeconds(flashDuration);

        // Eski renge dön
        for (int i = 0; i < _materials.Length; i++)
        {
            if (_materials[i] == null) continue;

            if (_materials[i].HasProperty("_BaseColor"))
                _materials[i].SetColor("_BaseColor", _originalColors[i]);
            else if (_materials[i].HasProperty("_Color"))
                _materials[i].SetColor("_Color", _originalColors[i]);
        }

        _isFlashing = false;
    }

    #endregion

    #region XP Drop

    private void SpawnXPBoxes()
    {
        if (xpPrefab == null) return;

        int count = Random.Range(xpBoxMin, xpBoxMax + 1);

        for (int i = 0; i < count; i++)
        {
            // Etrafına hafif random offset ile saç
            Vector3 offset = new Vector3(
                Random.Range(-xpSpawnRadius, xpSpawnRadius),
                0.1f,
                Random.Range(-xpSpawnRadius, xpSpawnRadius)
            );

            Vector3 spawnPos = transform.position + offset;
            Instantiate(xpPrefab, spawnPos, Quaternion.identity);
        }
    }

    #endregion

    #region GOLD Drop (Elite)

    private void SpawnGoldPieces()
    {
        if (goldPrefab == null) return;

        // 1 ile goldMaxPieces dahil arasında
        int count = Random.Range(goldMinPieces, goldMaxPieces + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = new Vector3(
                Random.Range(-goldSpawnRadius, goldSpawnRadius),
                0.1f,
                Random.Range(-goldSpawnRadius, goldSpawnRadius)
            );

            Vector3 spawnPos = transform.position + offset;
            Instantiate(goldPrefab, spawnPos, Quaternion.identity);
        }
    }

    #endregion

    #region ELITE / HEALTH HELPER

    /// <summary>
    /// Dışarıdan maksimum canı ayarlamak için.
    /// Örneğin spawner içinde: health.SetMaxHealth(150f);
    /// </summary>
    public void SetMaxHealth(float newMaxHealth, bool fullHeal = true)
    {
        maxHealth = newMaxHealth;
        if (fullHeal)
        {
            currentHealth = maxHealth;
        }
    }

    #endregion
}
