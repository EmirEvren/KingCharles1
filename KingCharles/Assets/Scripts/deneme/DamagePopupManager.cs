using UnityEngine;

public class DamagePopupManager : MonoBehaviour
{
    public static DamagePopupManager Instance;

    [Header("Prefab")]
    public DamagePopup damagePopupPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Verilen dünyasal konumda hasar popup'ý spawn eder.
    /// </summary>
    public void ShowDamage(float damageAmount, Vector3 worldPosition, Color color)
    {
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("[DamagePopupManager] damagePopupPrefab atanmadý!");
            return;
        }

        // Düþmanýn biraz üstünde görünsün
        Vector3 spawnPos = worldPosition + Vector3.up * 1.5f;

        DamagePopup popup = Instantiate(damagePopupPrefab, spawnPos, Quaternion.identity);
        popup.Setup(damageAmount, color);
    }

    /// <summary>
    /// Kolay statik çaðrý: DamagePopupManager.Show(...)
    /// </summary>
    public static void Show(float damageAmount, Vector3 worldPosition, Color color)
    {
        if (Instance == null)
        {
            Debug.LogWarning("[DamagePopupManager] Instance yok, sahneye DamagePopupManager eklemeyi unutma!");
            return;
        }

        Instance.ShowDamage(damageAmount, worldPosition, color);
    }
}
