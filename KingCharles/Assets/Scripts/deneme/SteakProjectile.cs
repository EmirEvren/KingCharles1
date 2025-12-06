using UnityEngine;

public class SteakProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 25f;
    public float turnSpeed = 15f;
    public float lifeTime = 3f;

    [Header("Spin")]
    public float spinSpeed = 360f;   // İstersen 0 yaparsın, dönmez

    [Header("Hasar")]
    public float damage = 25f;       // Kemik gibi 25, istersen arttır

    [Header("Vuruş Alanı (Circle)")]
    public float hitRadius = 1.5f;   // Düşmanın etrafındaki daire

    [Header("Sesler")]
    public AudioClip flightSfx;      // Havada giderken
    public AudioClip hitSfx;         // Düşmana çarpınca
    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;

    private AudioSource audioSource; // Uçuş sesi için (Ana AudioSource)
    private Vector3 moveDir;
    private Transform target;

    private void Awake()
    {
        // AudioSource hazırla (yoksa ekler)
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

        // Uçuş sesi
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
            Vector3 steakPos = transform.position;
            Vector3 enemyPos = target.position;
            steakPos.y = 0f;
            enemyPos.y = 0f;

            float sqrDist = (enemyPos - steakPos).sqrMagnitude;
            float sqrHitRadius = hitRadius * hitRadius;

            if (sqrDist <= sqrHitRadius)
            {
                DoHit();
                return;
            }
        }

        // Spin
        if (spinSpeed != 0f)
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

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
            GameObject tempAudioObj = new GameObject("TempSteakHitSFX");
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
        if (target != null)
        {
            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = target.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                float finalDamage = damage;

                if (WeaponChoiceManager.Instance != null)
                {
                    finalDamage = WeaponChoiceManager.Instance.GetModifiedDamage(WeaponType.Steak, damage);
                }

                enemyHealth.TakeDamage(finalDamage);
            }
        }

        Destroy(gameObject);
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
