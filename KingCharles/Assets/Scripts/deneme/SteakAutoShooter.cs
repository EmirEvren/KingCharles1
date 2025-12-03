using UnityEngine;

public class SteakAutoShooter : MonoBehaviour
{
    [Header("Biftek Ayarları")]
    public GameObject steakPrefab;   // Biftek prefab
    public Transform firePoint;      // Çıkış noktası
    public float attackRange = 15f;  // En yakındaki düşmanı bu mesafede arar
    public float fireRate = 1.5f;    // Saniyede kaç atış (1.5 → ~0.66 sn'de bir)

    private float fireCooldown;

    private void Update()
    {
        fireCooldown -= Time.deltaTime;
        if (fireCooldown > 0f) return;

        Transform nearestEnemy = FindNearestEnemy();

        if (nearestEnemy != null)
        {
            fireCooldown = 1f / fireRate;
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

        Vector3 dir = target.position - firePoint.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
            dir = transform.forward;

        Quaternion rot = Quaternion.LookRotation(dir.normalized);

        GameObject go = Instantiate(steakPrefab, firePoint.position, rot);

        SteakProjectile proj = go.GetComponent<SteakProjectile>();
        if (proj != null)
        {
            proj.SetTarget(target);
            proj.SetDirection(dir.normalized);
        }
    }
}
