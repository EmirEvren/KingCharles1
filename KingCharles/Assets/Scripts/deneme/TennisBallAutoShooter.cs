using UnityEngine;

public class TennisBallAutoShooter : MonoBehaviour
{
    [Header("Tenis Topu Ayarları")]
    public GameObject tennisBallPrefab;   // Tenis topu prefab
    public Transform firePoint;           // Çıkış noktası
    public float attackRange = 15f;       // En yakındaki düşmanı bu mesafede arar
    public float fireRate = 2f;           // Saniyede kaç atış (upgrade öncesi)

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
                float mul = WeaponChoiceManager.Instance.GetAttackSpeedMultiplier(WeaponType.TennisBall);
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
        if (tennisBallPrefab == null || firePoint == null) return;

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
            extraCount = WeaponChoiceManager.Instance.GetExtraCount(WeaponType.TennisBall);
        }

        int totalProjectiles = 1 + extraCount;

        for (int i = 0; i < totalProjectiles; i++)
        {
            GameObject go = Instantiate(tennisBallPrefab, firePoint.position, rot);

            TennisBallProjectile proj = go.GetComponent<TennisBallProjectile>();
            if (proj != null)
            {
                // DAMAGE UPGRADE → proj.damage’a yaz
                float baseDamage = proj.damage;
                if (WeaponChoiceManager.Instance != null)
                {
                    float modified = WeaponChoiceManager.Instance.GetModifiedDamage(WeaponType.TennisBall, baseDamage);
                    proj.damage = modified;
                }

                proj.SetTarget(target);
                proj.SetDirection(dir);
            }
        }

        Debug.Log($"[TennisBallAutoShooter] Fired {totalProjectiles} tennis balls.");
    }
}
