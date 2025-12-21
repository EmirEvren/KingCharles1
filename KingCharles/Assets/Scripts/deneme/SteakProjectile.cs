using System.Collections.Generic;
using UnityEngine;

public class SteakProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 25f;
    public float turnSpeed = 15f;
    public float lifeTime = 3f;

    [Header("Spin")]
    public float spinSpeed = 360f;

    [Header("Hasar")]
    public float damage = 25f;

    [Header("Vuruş Alanı (Circle)")]
    public float hitRadius = 1.5f;

    [Header("Ricochet (Sekme)")]
    public float ricochetSearchRadius = 8f;

    [Header("Sesler")]
    public AudioClip flightSfx;
    public AudioClip hitSfx;
    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;

    private AudioSource audioSource;
    private Vector3 moveDir;
    private Transform target;

    private int ricochetsRemaining = 0;
    private HashSet<int> hitEnemyIds = new HashSet<int>();

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        IgnoreCameraCollision();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);

        if (moveDir.sqrMagnitude < 0.001f)
            moveDir = transform.forward;

        if (PlayerPermanentUpgrades.Instance != null)
            ricochetsRemaining = Mathf.Max(0, PlayerPermanentUpgrades.Instance.ricochetBounces);

        if (flightSfx != null)
        {
            audioSource.clip = flightSfx;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void Update()
    {
        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                moveDir = Vector3.Slerp(moveDir, dir, Time.deltaTime * turnSpeed);
            }

            Vector3 projPos = transform.position;
            Vector3 enemyPos = target.position;
            projPos.y = 0f;
            enemyPos.y = 0f;

            float sqrDist = (enemyPos - projPos).sqrMagnitude;
            float sqrHitRadius = hitRadius * hitRadius;

            if (sqrDist <= sqrHitRadius)
            {
                DoHit();
                return;
            }
        }

        if (spinSpeed != 0f)
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        transform.position += moveDir * speed * Time.deltaTime;
    }

    public void SetTarget(Transform t)
    {
        target = t;
    }

    public void SetDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.001f)
            moveDir = dir.normalized;
        else
            moveDir = transform.forward;
    }

    private void DoHit()
    {
        // --- SES ---
        if (hitSfx != null)
        {
            GameObject tempAudioObj = new GameObject("TempSteakHitSFX");
            tempAudioObj.transform.position = transform.position;

            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = hitSfx;
            tempSource.volume = hitSfxVolume;
            tempSource.spatialBlend = 1f;

            if (audioSource != null && audioSource.outputAudioMixerGroup != null)
                tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;

            tempSource.Play();
            Destroy(tempAudioObj, hitSfx.length);
        }

        // --- HASAR ---
        EnemyHealth enemyHealth = null;

        if (target != null)
        {
            hitEnemyIds.Add(target.gameObject.GetInstanceID());

            enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = target.GetComponentInParent<EnemyHealth>();
        }

        if (enemyHealth != null)
        {
            enemyHealth.TakeDamage(damage);
        }

        // --- RICOCHET ---
        if (ricochetsRemaining > 0)
        {
            Transform next = FindNextEnemy(transform.position, ricochetSearchRadius);
            if (next != null)
            {
                ricochetsRemaining--;

                target = next;

                Vector3 newDir = (target.position - transform.position);
                newDir.y = 0f;
                if (newDir.sqrMagnitude < 0.001f) newDir = transform.forward;
                moveDir = newDir.normalized;

                transform.position += moveDir * 0.25f;

                return;
            }
        }

        Destroy(gameObject);
    }

    private Transform FindNextEnemy(Vector3 fromPos, float radius)
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Transform best = null;
        float bestSqr = Mathf.Infinity;

        float rSqr = radius * radius;
        Vector3 p = fromPos;
        p.y = 0f;

        foreach (GameObject e in enemies)
        {
            if (e == null || !e.activeInHierarchy) continue;

            int id = e.GetInstanceID();
            if (hitEnemyIds.Contains(id)) continue;

            Vector3 ep = e.transform.position;
            ep.y = 0f;

            float sqr = (ep - p).sqrMagnitude;
            if (sqr <= rSqr && sqr < bestSqr)
            {
                bestSqr = sqr;
                best = e.transform;
            }
        }

        return best;
    }

    private void IgnoreCameraCollision()
    {
        Collider[] myColliders = GetComponentsInChildren<Collider>();
        if (myColliders == null || myColliders.Length == 0) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        Collider camCol = cam.GetComponent<Collider>();
        if (camCol == null)
            camCol = cam.GetComponentInParent<Collider>();
        if (camCol == null) return;

        foreach (var col in myColliders)
        {
            if (col != null)
                Physics.IgnoreCollision(col, camCol, true);
        }
    }
}
