using UnityEngine;

public class EnemyHealth : MonoBehaviour
{
    [Header("Health Ayarlarý")]
    public float maxHealth = 50f;      // Inspector'dan deðiþtirebilirsin, default 50
    [SerializeField] private float currentHealth;

    [Header("Opsiyonel Animasyon")]
    public Animator animator;          // Ölüm animasyonu için istersen baðla
    public string deathTriggerName = "Death"; // Animator'daki Death trigger ismi

    private bool isDead = false;

    [Header("Vurulma Efekti (Hit Flash)")]
    public Renderer[] renderersToFlash;    // Boþ býrakýrsan otomatik bulur
    public Color hitColor = Color.red;     // Vurulduðunda olacak renk
    public float flashDuration = 0.1f;     // Ne kadar süre kýrmýzý kalsýn

    private Material[] _materials;
    private Color[] _originalColors;
    private bool _isFlashing = false;

    private void Awake()
    {
        // Baþlangýçta caný maksimuma eþitle
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();

        // Renderer'larý ayarla
        if (renderersToFlash == null || renderersToFlash.Length == 0)
        {
            // Düþmanýn çocuklarýndaki tüm Renderer'larý otomatik bul
            renderersToFlash = GetComponentsInChildren<Renderer>();
        }

        // Materyal ve orijinal renkleri cache'le
        _materials = new Material[renderersToFlash.Length];
        _originalColors = new Color[renderersToFlash.Length];

        for (int i = 0; i < renderersToFlash.Length; i++)
        {
            // Her enemy için ayrý material instance alýyoruz
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
    /// Bu fonksiyon dýþardan çaðrýlacak. (Oyuncu vurduðunda vs.)
    /// </summary>
    public void TakeDamage(float amount)
    {
        if (isDead) return;

        currentHealth -= amount;

        // Her damage aldýðýnda kýrmýzý flash yapsýn
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

        // Animasyon süresine göre yok et (2 saniye sonra)
        Destroy(gameObject, 2f);
    }

    // Ýstersen baþka scriptlerden okunabilsin
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

        // Kýrmýzý yap
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
}
