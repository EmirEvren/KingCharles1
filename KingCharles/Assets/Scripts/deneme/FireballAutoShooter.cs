using System.Collections.Generic;
using UnityEngine;

public class FireballAutoShooter : MonoBehaviour
{
    [Header("Fireball Ayarlarý")]
    public GameObject fireballPrefab;  // Fireball prefabýný buraya sürükle
    public Transform firePoint;        // Çýkýþ noktasý
    public float attackRange = 15f;    // En yakýndaki düþmaný bu mesafe içinde arar
    public float fireRate = 1.5f;      // Saniyede kaç atýþ (1.5 -> ~0.66 sn’de bir, upgrade öncesi)

    private float fireCooldown;

    private void Update()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        Transform nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            // Fire rate'i upgrade'den gelen çarpanla buff'la
            float finalFireRate = fireRate;

            if (WeaponChoiceManager.Instance != null)
            {
                float mul = WeaponChoiceManager.Instance.GetAttackSpeedMultiplier(WeaponType.Fireball);
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
        if (fireballPrefab == null || firePoint == null) return;

        // Hedef yönü
        Vector3 dir = target.position - firePoint.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;

        dir.Normalize();
        Quaternion rot = Quaternion.LookRotation(dir);

        // Fazladan mermi sayýsýný upgrade sisteminden çek
        int extraCount = 0;
        if (WeaponChoiceManager.Instance != null)
        {
            extraCount = WeaponChoiceManager.Instance.GetExtraCount(WeaponType.Fireball);
        }

        int totalProjectiles = 1 + extraCount;

        for (int i = 0; i < totalProjectiles; i++)
        {
            GameObject go = Instantiate(fireballPrefab, firePoint.position, rot);

            // --- LÝMÝTLEYÝCÝ TOKEN: ayný anda en fazla 10 tane görünsün/ses versin ---
            go.AddComponent<FireballProjectileVisibilityLimiterToken>();
            // -----------------------------------------------------------------------

            FireballProjectile proj = go.GetComponent<FireballProjectile>();
            if (proj != null)
            {
                // ---- HASARI BURADA UYGULA ----
                float baseDamage = proj.damage;
                if (WeaponChoiceManager.Instance != null)
                {
                    float modified = WeaponChoiceManager.Instance.GetModifiedDamage(WeaponType.Fireball, baseDamage);
                    proj.damage = modified; // Artýk component'in damage field'i buff’lý
                    Debug.Log($"[FireballAutoShooter] Fireball projectile damage set to {proj.damage}");
                }

                proj.SetTarget(target);
                proj.SetDirection(dir);
            }
        }

        Debug.Log($"[FireballAutoShooter] Fired {totalProjectiles} fireballs.");
    }
}

/// <summary>
/// Sahnede ayný anda en fazla N FireballProjectile görünsün ve ses çýkarsýn.
/// N üstündekiler görünmez + sessiz olur.
/// Bir tanesi yok olunca sýradaki görünür olur.
/// </summary>
public class FireballProjectileVisibilityLimiterToken : MonoBehaviour
{
    private const int MAX_VISIBLE = 10;

    private static readonly List<FireballProjectileVisibilityLimiterToken> Active = new List<FireballProjectileVisibilityLimiterToken>();

    private Renderer[] cachedRenderers;
    private AudioSource[] cachedAudioSources;

    private FireballProjectile proj;
    private AudioClip cachedHitSfx;

    private bool isVisible = true;

    private void Awake()
    {
        // Cache components
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        cachedAudioSources = GetComponentsInChildren<AudioSource>(true);

        proj = GetComponent<FireballProjectile>();
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
        // null temizliði
        for (int i = Active.Count - 1; i >= 0; i--)
        {
            if (Active[i] == null) Active.RemoveAt(i);
        }

        // Ýlk 10 görünür, geri kalaný gizli
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

        // --- Hit SFX kapat/aç (gizliyken vurunca da ses çýkmasýn) ---
        if (proj != null)
        {
            if (visible)
                proj.hitSfx = cachedHitSfx;
            else
                proj.hitSfx = null;
        }
    }
}
