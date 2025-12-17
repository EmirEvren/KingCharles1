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

    [Header("Damage Popup")]
    public bool showDamagePopup = true;
    public Color popupColor = Color.white;

    [Header("XP Drop")]
    public GameObject xpPrefab;        // XP kutusu prefabı
    public int xpBoxMin = 1;           // Min kutu sayısı
    public int xpBoxMax = 3;           // Max kutu sayısı
    public float xpSpawnRadius = 0.5f; // Etrafına ne kadar saçılacak

    [Header("Gold Drop (Sadece Elite)")]
    public GameObject goldPrefab;         // Altın parçası prefabı
    public int goldMinPieces = 1;         // Elite başına minimum altın parçası
    public int goldMaxPieces = 5;         // Elite başına maksimum altın parçası (istenen: 5)
    public float goldSpawnRadius = 0.5f;  // Altınların etrafa saçılma yarıçapı

    [Header("Heart Drop (1/25)")]
    public GameObject heartPrefab;         // Kalp pickup prefabı
    public float heartSpawnRadius = 0.5f;  // Kalbin etrafa saçılma yarıçapı
    public float heartSpawnY = 0.1f;       // Yerden biraz yukarı

    [Header("MiniBoss Override Drop")]
    public bool isMiniBoss = false;      // Spawner true yapacak
    public int miniBossTotalXP = 100;    // Toplam XP
    public int miniBossTotalGold = 100;  // Toplam Gold

    [Header("Hit Flash")]
    public float flashDuration = 0.05f;    // Her blink süresi
    public int flashCount = 2;             // Kaç kere blink (2 => beyaz->normal->beyaz->normal)
    public Color flashColor = Color.white; // Flash rengi

    private Renderer[] _renderers;
    private Material[] _materials;
    private Color[] _originalColors;
    private bool _isFlashing = false;

    private void Awake()
    {
        // Başlangıçta canı maksimuma eşitle
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        // Hit flash için renderer/material cache
        _renderers = GetComponentsInChildren<Renderer>(true);

        if (_renderers != null && _renderers.Length > 0)
        {
            _materials = new Material[_renderers.Length];
            _originalColors = new Color[_renderers.Length];

            for (int i = 0; i < _renderers.Length; i++)
            {
                // material -> instance oluşturur (flash için güvenli)
                _materials[i] = _renderers[i].material;

                if (_materials[i] != null && _materials[i].HasProperty("_Color"))
                    _originalColors[i] = _materials[i].color;
                else
                    _originalColors[i] = Color.white;
            }
        }
    }

    /// <summary>
    /// Düşman hasar alır.
    /// </summary>
    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        if (currentHealth < 0f) currentHealth = 0f;

        // Damage Popup
        if (showDamagePopup && DamagePopupManager.Instance != null)
        {
            DamagePopupManager.Show(dmg, transform.position, popupColor);
        }

        // Hit flash
        StartHitFlash();

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Ölüm animasyonu varsa tetikle
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

        // Elite mi? maxHealth >= 150 ise ELITE kabul ediyoruz
        bool isEliteKill = maxHealth >= 150f;

        if (isMiniBoss)
        {
            // Miniboss: toplam 100 XP + toplam 100 Gold
            SpawnTotalXP(miniBossTotalXP);
            SpawnTotalGold(miniBossTotalGold);

            // İstersen miniboss da kalp düşürebilir (mevcut sistem)
            TrySpawnHeart();
        }
        else
        {
            // Normal enemy: random XP kutuları
            SpawnXPBoxes();
            TrySpawnHeart();

            // Elite ise altın parçalarını spawn et
            if (isEliteKill)
            {
                SpawnGoldPieces();
            }
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
        if (_materials == null || _materials.Length == 0)
            yield break;

        _isFlashing = true;

        for (int c = 0; c < flashCount; c++)
        {
            // Flash rengi
            for (int i = 0; i < _materials.Length; i++)
            {
                if (_materials[i] != null && _materials[i].HasProperty("_Color"))
                    _materials[i].color = flashColor;
            }

            yield return new WaitForSeconds(flashDuration);

            // Orijinal renge dön
            for (int i = 0; i < _materials.Length; i++)
            {
                if (_materials[i] != null && _materials[i].HasProperty("_Color"))
                    _materials[i].color = _originalColors[i];
            }

            yield return new WaitForSeconds(flashDuration);
        }

        // Son garanti: orijinal renk
        for (int i = 0; i < _materials.Length; i++)
        {
            if (_materials[i] != null && _materials[i].HasProperty("_Color"))
                _materials[i].color = _originalColors[i];
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

    private void SpawnTotalXP(int totalXP)
    {
        if (xpPrefab == null) return;
        if (totalXP <= 0) return;

        int per = 1;
        XPPickup sample = xpPrefab.GetComponent<XPPickup>();
        if (sample != null) per = Mathf.Max(1, sample.xpAmount);

        int fullCount = totalXP / per;
        int remainder = totalXP % per;

        for (int i = 0; i < fullCount; i++)
            SpawnXPAmount(per);

        if (remainder > 0)
            SpawnXPAmount(remainder);
    }

    private void SpawnXPAmount(int amount)
    {
        Vector3 offset = new Vector3(
            Random.Range(-xpSpawnRadius, xpSpawnRadius),
            0.1f,
            Random.Range(-xpSpawnRadius, xpSpawnRadius)
        );

        Vector3 spawnPos = transform.position + offset;
        GameObject go = Instantiate(xpPrefab, spawnPos, Quaternion.identity);

        XPPickup xp = go.GetComponent<XPPickup>();
        if (xp != null) xp.xpAmount = amount;
    }

    #endregion

    private void TrySpawnHeart()
    {
        if (heartPrefab == null) return;

        if (Random.Range(0, 10) != 0) return;

        Vector3 offset = new Vector3(
            Random.Range(-heartSpawnRadius, heartSpawnRadius),
            heartSpawnY,
            Random.Range(-heartSpawnRadius, heartSpawnRadius)
        );

        Instantiate(heartPrefab, transform.position + offset, Quaternion.identity);
    }

    #region GOLD Drop (Elite)

    private void SpawnGoldPieces()
    {
        if (goldPrefab == null) return;

        // 1 ile goldMaxPieces dahil arasında
        int count = Random.Range(goldMinPieces, goldMaxPieces + 1);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset =
                new Vector3(
                    Random.Range(-goldSpawnRadius, goldSpawnRadius),
                    0.1f,
                    Random.Range(-goldSpawnRadius, goldSpawnRadius)
                );

            Vector3 spawnPos = transform.position + offset;
            Instantiate(goldPrefab, spawnPos, Quaternion.identity);
        }
    }

    private void SpawnTotalGold(int totalGold)
    {
        if (goldPrefab == null) return;
        if (totalGold <= 0) return;

        int per = 1;
        GoldPickup sample = goldPrefab.GetComponent<GoldPickup>();
        if (sample != null) per = Mathf.Max(1, sample.goldValue);

        int fullCount = totalGold / per;
        int remainder = totalGold % per;

        for (int i = 0; i < fullCount; i++)
            SpawnGoldAmount(per);

        if (remainder > 0)
            SpawnGoldAmount(remainder);
    }

    private void SpawnGoldAmount(int amount)
    {
        Vector3 offset = new Vector3(
            Random.Range(-goldSpawnRadius, goldSpawnRadius),
            0.1f,
            Random.Range(-goldSpawnRadius, goldSpawnRadius)
        );

        Vector3 spawnPos = transform.position + offset;
        GameObject go = Instantiate(goldPrefab, spawnPos, Quaternion.identity);

        GoldPickup g = go.GetComponent<GoldPickup>();
        if (g != null) g.goldValue = amount;
    }

    #endregion

    #region SetMaxHealth

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
