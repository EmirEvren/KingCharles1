using UnityEngine;

public class FireballProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 20f;        // Fireball biraz daha yavaş olabilir, istersen arttır
    public float turnSpeed = 10f;    // Hedefe dönerken dönüş hızı
    public float lifeTime = 4f;      // Havada maksimum kalma süresi

    [Header("Spin")]
    public float spinSpeed = 0f;     // İstersen burada da spin verebilirsin (örn: 360)

    [Header("Hasar")]
    public float damage = 25f;       // Base damage (kemikle aynı, istersen artır)

    [Header("Vuruş Alanı (Circle)")]
    public float hitRadius = 1.5f;   // Düşmanın etrafındaki daire yarıçapı

    [Header("Sesler")]
    public AudioClip flightSfx;      // Havada giderken çalan ses (loop)
    public AudioClip hitSfx;         // Düşmana çarpınca çalan ses
    public float hitSfxVolume = 1f;  // Çarpma sesi volume

    private AudioSource audioSource;
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
        // Çarpma sesi
        if (hitSfx != null)
        {
            AudioSource.PlayClipAtPoint(hitSfx, transform.position, hitSfxVolume);
        }

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

}
