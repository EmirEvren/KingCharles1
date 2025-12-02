using UnityEngine;

public class BoneAutoShooter : MonoBehaviour
{
    [Header("Kemik Atýþ Ayarlarý")]
    public GameObject bonePrefab;   // Fýrlatýlacak kemik prefabý
    public Transform firePoint;     // Kemiðin çýkacaðý nokta
    public float attackRange = 15f; // En yakýndaki düþmaný bu mesafe içinde arayacak
    public float fireRate = 2f;     // Saniyede kaç kere fire (2 => 0.5 sn'de bir)

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
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy"); // Tüm düþmanlarda "Enemy" tag'i olmalý
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

        Quaternion rot = Quaternion.LookRotation(dir.normalized);

        // Kemik spawn
        GameObject bone = Instantiate(bonePrefab, firePoint.position, rot);

        // Kemiðe hedef ver
        BoneProjectile proj = bone.GetComponent<BoneProjectile>();
        if (proj != null)
        {
            proj.SetTarget(target);          // HOMING için hedef ver
            proj.SetDirection(dir.normalized); // Hedef yok olursa düz devam etsin
        }
    }
}
