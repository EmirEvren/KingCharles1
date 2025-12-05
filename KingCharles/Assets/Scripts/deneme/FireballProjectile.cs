using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 20f;        // Fireball hızı
    public float turnSpeed = 10f;    // Hedefe dönerken dönüş hızı
    public float lifeTime = 4f;      // Havada maksimum kalma süresi

    [Header("Spin")]
    public float spinSpeed = 0f;     // İstersen burada da spin verebilirsin

    [Header("Hasar")]
    public float damage = 25f;       // Base damage

    [Header("Vuruş Alanı (Circle)")]
    public float hitRadius = 1.5f;   // Düşmanın etrafındaki daire yarıçapı

    [Header("Sesler")]
    public AudioClip flightSfx;      // Havada giderken çalan ses (loop)
    public AudioClip hitSfx;         // Düşmana çarpınca çalan ses
    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;  // Çarpma sesi volume

    private AudioSource audioSource; // Uçuş sesi için (Main AudioSource)
    private Vector3 moveDir;
    private Transform target;

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
            Vector3 firePos = transform.position;
            Vector3 enemyPos = target.position;
            firePos.y = 0f;
            enemyPos.y = 0f;

            float sqrDist = (enemyPos - firePos).sqrMagnitude;
            float sqrHitRadius = hitRadius * hitRadius;

            if (sqrDist <= sqrHitRadius)
            {
                DoHit();
                return;
            }
        }

        // İstersen spin ver
        if (spinSpeed != 0f)
        {
            transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);
        }

        // İleri hareket
        transform.position += moveDir * speed * Time.deltaTime;
    }

    // Otomatik shooter buradan hedef veriyor
    public void SetTarget(Transform t)
    {
        target = t;
    }

    // Otomatik shooter ilk yönü buradan veriyor
    public void SetDirection(Vector3 dir)
    {
        if (dir.sqrMagnitude > 0.001f)
            moveDir = dir.normalized;
        else
            moveDir = transform.forward;
    }

    private void DoHit()
    {
        // --- SES DÜZELTMESİ BAŞLANGIÇ ---
        // Çarpma sesi (Mixer Ayarlı)
        if (hitSfx != null)
        {
            // 1. Geçici obje oluştur
            GameObject tempAudioObj = new GameObject("TempFireballHitSFX");
            tempAudioObj.transform.position = transform.position;

            // 2. AudioSource ekle ve ayarla
            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = hitSfx;
            tempSource.volume = hitSfxVolume;
            tempSource.spatialBlend = 1f; // 3D ses

            // 3. KRİTİK: Ana objenin Mixer Grubunu kopyala
            // (Unity Editörde bu objenin AudioSource Output'una SFX atamayı unutma)
            if (audioSource != null && audioSource.outputAudioMixerGroup != null)
            {
                tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            }

            // 4. Çal ve yok et
            tempSource.Play();
            Destroy(tempAudioObj, hitSfx.length);
        }
        // --- SES DÜZELTMESİ BİTİŞ ---

        // Hasar ver
        if (target != null)
        {
            EnemyHealth enemyHealth = target.GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = target.GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.TakeDamage(damage);
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