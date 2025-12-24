using System.Collections.Generic;
using UnityEngine;

public class SteakAutoShooter : MonoBehaviour
{
    [Header("Biftek Ayarları")]
    public GameObject steakPrefab;   // Biftek prefab
    public Transform firePoint;      // Çıkış noktası
    public float attackRange = 15f;  // En yakındaki düşmanı bu mesafede arar
    public float fireRate = 1.5f;    // Saniyede kaç atış (1.5 → ~0.66 sn'de bir, upgrade öncesi)

    private float fireCooldown;

    private void Update()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        Transform nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            // Fire rate'i upgrade'den gelen çarpanla buff’la
            float finalFireRate = fireRate;

            if (WeaponChoiceManager.Instance != null)
            {
                float mul = WeaponChoiceManager.Instance.GetAttackSpeedMultiplier(WeaponType.Steak);
                finalFireRate *= mul;
            }

            if (finalFireRate <= 0f) finalFireRate = 0.01f;

            fireCooldown = 1f / finalFireRate;

            ShootAt(nearestEnemy);
        }
    }

    private Transform FindNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform nearest = null;
        float nearestSqrDist = Mathf.Infinity;
        Vector3 myPos = transform.position;

        foreach (GameObject e in enemies)
        {
            if (!e.activeInHierarchy) continue;

            Vector3 diff = e.transform.position - myPos;
            float sqr = diff.sqrMagnitude;

            if (sqr < nearestSqrDist && sqr <= attackRange * attackRange)
            {
                nearestSqrDist = sqr;
                nearest = e.transform;
            }
        }

        return nearest;
    }

    private void ShootAt(Transform target)
    {
        if (steakPrefab == null || firePoint == null) return;

        // Hedef yönü
        Vector3 dir = target.position - firePoint.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;

        dir.Normalize();
        Quaternion rot = Quaternion.LookRotation(dir);

        // Fazladan mermi sayısını upgrade sisteminden çek
        int extraCount = 0;
        if (WeaponChoiceManager.Instance != null)
        {
            extraCount = WeaponChoiceManager.Instance.GetExtraCount(WeaponType.Steak);
        }

        int totalProjectiles = 1 + extraCount;

        for (int i = 0; i < totalProjectiles; i++)
        {
            GameObject go = Instantiate(steakPrefab, firePoint.position, rot);

            // --- LİMİTLEYİCİ TOKEN: aynı anda en fazla 10 tane görünsün/ses versin ---
            go.AddComponent<SteakProjectileVisibilityLimiterToken>();
            // -----------------------------------------------------------------------

            SteakProjectile proj = go.GetComponent<SteakProjectile>();
            if (proj != null)
            {
                // ---- HASARI BURADA UYGULA ----
                float baseDamage = proj.damage;
                if (WeaponChoiceManager.Instance != null)
                {
                    float modified = WeaponChoiceManager.Instance.GetModifiedDamage(WeaponType.Steak, baseDamage);
                    proj.damage = modified; // Artık component'in damage field'i buff’lı
                    Debug.Log($"[SteakAutoShooter] Steak projectile damage set to {proj.damage}");
                }

                proj.SetTarget(target);
                proj.SetDirection(dir);
            }
        }

        Debug.Log($"[SteakAutoShooter] Fired {totalProjectiles} steaks.");
    }
}

/// <summary>
/// Sahnede aynı anda en fazla N SteakProjectile görünsün ve ses çıkarsın.
/// N üstündekiler görünmez + sessiz olur.
/// Bir tanesi yok olunca sıradaki görünür olur.
/// </summary>
public class SteakProjectileVisibilityLimiterToken : MonoBehaviour
{
    private const int MAX_VISIBLE = 10;

    private static readonly List<SteakProjectileVisibilityLimiterToken> Active = new List<SteakProjectileVisibilityLimiterToken>();

    private Renderer[] cachedRenderers;
    private AudioSource[] cachedAudioSources;

    private SteakProjectile proj;
    private AudioClip cachedHitSfx;

    private bool isVisible = true;

    private void Awake()
    {
        // Cache components
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedAudioSources = GetComponentsInChildren<AudioSource>(true);

        proj = GetComponent<SteakProjectile>();
        if (proj != null)
        {
            cachedHitSfx = proj.hitSfx;
        }

        // Register
        Active.Add(this);
        RefreshAll();
    }

    private void OnDestroy()
    {
        Active.Remove(this);
        RefreshAll();
    }

    private static void RefreshAll()
    {
        // null temizliği
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            if (Active[i] == null) Active.RemoveAt(i);
        }

        // İlk 10 görünür, geri kalanı gizli
        for (int i = 0; i < Active.Count; i++)
        {
            bool shouldBeVisible = (i < MAX_VISIBLE);
            Active[i].SetVisible(shouldBeVisible);
        }
    }

    private void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        isVisible = visible;

        // --- Renderer kapat/aç ---
        if (cachedRenderers != null)
        {
            for (int i = 0; i < cachedRenderers.Length; i++)
            {
                if (cachedRenderers[i] != null)
                    cachedRenderers[i].enabled = visible;
            }
        }

        // --- Ses kapat/aç (mute) ---
        if (cachedAudioSources != null)
        {
            for (int i = 0; i < cachedAudioSources.Length; i++)
            {
                if (cachedAudioSources[i] != null)
                    cachedAudioSources[i].mute = !visible;
            }
        }

        // --- Hit SFX kapat/aç (gizliyken vurunca da ses çıkmasın) ---
        if (proj != null)
        {
            if (visible)
                proj.hitSfx = cachedHitSfx;
            else
                proj.hitSfx = null;
        }
    }
}
