using System.Collections.Generic;
using UnityEngine;

public class TennisBallProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 22f;        // İstersen değiştir
    public float turnSpeed = 14f;    // Hedefe dönerken dönüş hızı
    public float lifeTime = 3.5f;    // Havada maksimum kalma süresi

    [Header("Spin")]
    public float spinSpeed = 540f;   // Tenis topu daha hızlı dönebilir :)

    [Header("Hasar")]
    public float damage = 25f;       // Base damage (upgrade öncesi)

    [Header("Vuruş Alanı (Circle)")]
    public float hitRadius = 1.5f;   // Düşmanın etrafındaki daire yarıçapı

    [Header("Ricochet (Sekme)")]
    public float ricochetSearchRadius = 8f; // Sekmede yeni hedef arama yarıçapı

    [Header("Sesler")]
    public AudioClip flightSfx;      // Havada giderken çalan ses (loop)
    public AudioClip hitSfx;         // Düşmana çarpınca çalan ses
    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;  // Çarpma sesi volume

    private AudioSource audioSource; // Uçuş sesi için (Ana AudioSource)
    private Vector3 moveDir;
    private Transform target;

    // Sekme state
    private int ricochetsRemaining = 0;                    // Kaç sekme kaldı
    private HashSet<int> hitEnemyIds = new HashSet<int>(); // Aynı düşmana tekrar vurmayı engelle

    private void Awake()
    {
        // AudioSource hazırla (yoksa ekle)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D ses
        }

        IgnoreCameraCollision();
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);

        if (moveDir.sqrMagnitude < 0.001f)
            moveDir = transform.forward;

        // Sekme hakkını (eşya varsa) al
        ricochetsRemaining = GetRicochetBouncesFromPermanentUpgrades();

        // Uçuş sesini başlat
        if (flightSfx != null)
        {
            audioSource.clip = flightSfx;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    private void Update()
    {
        // Hedef varsa → homing
        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                moveDir = Vector3.Slerp(moveDir, dir, Time.deltaTime * turnSpeed);
            }

            // Circle içine girdi mi?
            Vector3 ballPos = transform.position;
            Vector3 enemyPos = target.position;
            ballPos.y = 0f;
            enemyPos.y = 0f;

            float sqrDist = (enemyPos - ballPos).sqrMagnitude;
            float sqrHitRadius = hitRadius * hitRadius;

            if (sqrDist <= sqrHitRadius)
            {
                DoHit();
                return;
            }
        }

        // Spin
        if (spinSpeed != 0f)
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        // İleri hareket
        transform.position += moveDir * speed * Time.deltaTime;
    }

    // Shooter buradan hedef veriyor
    public void SetTarget(Transform t)
    {
        target = t;
    }

    // Shooter ilk yönü buradan veriyor
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
            GameObject tempAudioObj = new GameObject("TempTennisHitSFX");
            tempAudioObj.transform.position = transform.position;

            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = hitSfx;
            tempSource.volume = hitSfxVolume;
            tempSource.spatialBlend = 1f; // 3D ses

            if (audioSource != null && audioSource.outputAudioMixerGroup != null)
            {
                tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            }

            tempSource.Play();
            Destroy(tempAudioObj, hitSfx.length);
        }

        // --- HASAR ---
        Transform hitTr = target;
        EnemyHealth enemyHealth = null;

        if (hitTr != null)
        {
            enemyHealth = hitTr.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = hitTr.GetComponentInParent<EnemyHealth>();
        }

        if (enemyHealth != null)
        {
            int id = enemyHealth.gameObject.GetInstanceID();
            hitEnemyIds.Add(id);

            Debug.Log($"[TennisBallProjectile] Damage field used: {damage}");
            enemyHealth.TakeDamage(damage);
        }

        // --- RICOCHET ---
        if (ricochetsRemaining > 0)
        {
            Transform next = FindNextEnemyTarget();
            if (next != null)
            {
                ricochetsRemaining--;

                target = next;

                Vector3 dir = (target.position - transform.position);
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                {
                    moveDir = dir.normalized;
                    transform.position += moveDir * 0.25f;
                }

                return; // yok olma, yeni hedefe devam
            }
        }

        Destroy(gameObject);
    }

    private Transform FindNextEnemyTarget()
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, ricochetSearchRadius);
        Transform best = null;
        float bestSqr = Mathf.Infinity;

        foreach (var c in cols)
        {
            if (c == null) continue;

            EnemyHealth eh = c.GetComponent<EnemyHealth>();
            if (eh == null) eh = c.GetComponentInParent<EnemyHealth>();
            if (eh == null) continue;

            GameObject go = eh.gameObject;
            if (!go.activeInHierarchy) continue;

            int id = go.GetInstanceID();
            if (hitEnemyIds.Contains(id)) continue;

            float sqr = (go.transform.position - transform.position).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = go.transform;
            }
        }

        return best;
    }

    private int GetRicochetBouncesFromPermanentUpgrades()
    {
        int result = 0;

        var pu = PlayerPermanentUpgrades.Instance;
        if (pu == null) return result;

        var t = pu.GetType();

        string[] intNames =
        {
            "ricochetBounces",
            "bulletBounceCount",
            "bounceCount",
            "stickyBoneBounces"
        };

        foreach (var name in intNames)
        {
            var f = t.GetField(name);
            if (f != null && f.FieldType == typeof(int))
                return Mathf.Max(0, (int)f.GetValue(pu));

            var p = t.GetProperty(name);
            if (p != null && p.PropertyType == typeof(int) && p.CanRead)
                return Mathf.Max(0, (int)p.GetValue(pu, null));
        }

        string[] boolNames =
        {
            "hasStickyBone",
            "stickyBone",
            "enableRicochet",
            "hasRicochet"
        };

        foreach (var name in boolNames)
        {
            var f = t.GetField(name);
            if (f != null && f.FieldType == typeof(bool) && (bool)f.GetValue(pu))
                return 2;

            var p = t.GetProperty(name);
            if (p != null && p.PropertyType == typeof(bool) && p.CanRead && (bool)p.GetValue(pu, null))
                return 2;
        }

        return result;
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
            {
                Physics.IgnoreCollision(col, camCol, true);
            }
        }
    }
}
