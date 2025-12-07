using UnityEngine;

public class BoneAutoShooter : MonoBehaviour
{
    [Header("Kemik Atýþ Ayarlarý")]
    public GameObject bonePrefab;   // Fýrlatýlacak kemik prefabý
    public Transform firePoint;     // Kemiðin çýkacaðý nokta
    public float attackRange = 15f; // En yakýndaki düþmaný bu mesafe içinde arayacak
    public float fireRate = 2f;     // Saniyede kaç kere fire (2 => 0.5 sn'de bir, upgrade öncesi)

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
                float mul = WeaponChoiceManager.Instance.GetAttackSpeedMultiplier(WeaponType.Bone);
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
        if (bonePrefab == null || firePoint == null) return;

        // Hedef yönü
        Vector3 dir = (target.position - firePoint.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;

        dir.Normalize();
        Quaternion rot = Quaternion.LookRotation(dir);

        // Fazladan mermi sayýsýný upgrade sisteminden çek
        int extraCount = 0;
        if (WeaponChoiceManager.Instance != null)
        {
            extraCount = WeaponChoiceManager.Instance.GetExtraCount(WeaponType.Bone);
        }

        int totalProjectiles = 1 + extraCount;

        for (int i = 0; i < totalProjectiles; i++)
        {
            GameObject bone = Instantiate(bonePrefab, firePoint.position, rot);

            BoneProjectile proj = bone.GetComponent<BoneProjectile>();
            if (proj != null)
            {
                // ---- HASARI BURADA UYGULA ----
                float baseDamage = proj.damage;
                if (WeaponChoiceManager.Instance != null)
                {
                    float modified = WeaponChoiceManager.Instance.GetModifiedDamage(WeaponType.Bone, baseDamage);
                    proj.damage = modified; // Artýk component'in damage field'i de buff’lý
                    Debug.Log($"[BoneAutoShooter] Bone projectile damage set to {proj.damage}");
                }

                proj.SetTarget(target);
                proj.SetDirection(dir);
            }
        }

        Debug.Log($"[BoneAutoShooter] Fired {totalProjectiles} bones.");
    }
}
