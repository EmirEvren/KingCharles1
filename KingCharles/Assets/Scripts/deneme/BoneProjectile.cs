using UnityEngine;

public class BoneProjectile : MonoBehaviour
{
    [Header("Hareket")]
    public float speed = 25f;
    public float turnSpeed = 15f;
    public float lifeTime = 3f;

    [Header("Spin")]
    public float spinSpeed = 360f;   // Y ekseninde saniyede 360 derece dönsün

    [Header("Hasar")]
    public float damage = 25f;       // Base 25 hasar

    [Header("Vuruş Alanı (Circle)")]
    public float hitRadius = 1.5f;   // Düşmanın etrafındaki daire yarıçapı

    [Header("Sesler")]
    public AudioClip flightSfx;      // Havada giderken çalan ses
    public AudioClip hitSfx;         // Düşmana çarpınca çalan ses
    public float hitSfxVolume = 1f;  // Çarpma sesi sesi seviyesi

    private AudioSource audioSource; // Uçuş sesi için

    private Vector3 moveDir;
    private Transform target;

    private void Awake()
    {
        // Aynı objede AudioSource varsa kullan, yoksa ekle
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D ses istiyorsan 1, 2D istiyorsan 0 yapabilirsin
        }
    }

    private void Start()
    {
        Destroy(gameObject, lifeTime);

        if (moveDir.sqrMagnitude < 0.001f)
            moveDir = transform.forward;

        // Uçuş sesi ayarla ve çal
        if (flightSfx != null)
        {
            audioSource.clip = flightSfx;
            audioSource.loop = true;     // Havada giderken sürekli çalsın
            audioSource.Play();
        }
    }

    private void Update()
    {
        // Hedef varsa → ona doğru döndür
        if (target != null)
        {
            Vector3 dir = target.position - transform.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.001f)
            {
                dir.Normalize();
                moveDir = Vector3.Slerp(moveDir, dir, Time.deltaTime * turnSpeed);
            }

            // Circle içine girdi mi kontrol et
            Vector3 bonePos = transform.position;
            Vector3 enemyPos = target.position;
            bonePos.y = 0f;
            enemyPos.y = 0f;

            float sqrDist = (enemyPos - bonePos).sqrMagnitude;
            float sqrHitRadius = hitRadius * hitRadius;

            if (sqrDist <= sqrHitRadius)
            {
                DoHit();
                return;
            }
        }

        // Kemik çıktığı andan itibaren sürekli spin
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.Self);

        // İleri hareket
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
        // Çarpma sesi çal
        if (hitSfx != null)
        {
            // Objeyi hemen sileceğimiz için sesi dışarıda çaldırıyoruz
            AudioSource.PlayClipAtPoint(hitSfx, transform.position, hitSfxVolume);
        }

        // Hasar ver
        if (target != null)
        {
            EnemyHealth enemyHealth = target.GetCurrentComponentInParents<EnemyHealth>();
        }

        Destroy(gameObject);
    }
}

// Küçük yardımcı extension (aynı script dosyasında kalabilir)
public static class ComponentExtensions
{
    public static T GetCurrentComponentInParents<T>(this Component comp) where T : Component
    {
        T c = comp.GetComponent<T>();
        if (c != null) return c;
        return comp.GetComponentInParent<T>();
    }
}
