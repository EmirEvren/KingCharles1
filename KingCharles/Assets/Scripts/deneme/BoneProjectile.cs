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
    [Range(0f, 1f)]
    public float hitSfxVolume = 1f;  // Çarpma sesi seviyesi

    private AudioSource audioSource; // Uçuş sesi için (Main AudioSource)

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
            audioSource.spatialBlend = 1f; // 3D ses
        }

        IgnoreCameraCollision();
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
        // --- DÜZELTİLEN KISIM BAŞLANGIÇ ---
        // Vuruş sesini çal (Mixer Grubunu destekleyecek şekilde)
        if (hitSfx != null)
        {
            // 1. Geçici bir obje oluştur
            GameObject tempAudioObj = new GameObject("TempHitSFX");
            tempAudioObj.transform.position = transform.position;

            // 2. AudioSource ekle ve ayarla
            AudioSource tempSource = tempAudioObj.AddComponent<AudioSource>();
            tempSource.clip = hitSfx;
            tempSource.volume = hitSfxVolume;
            tempSource.spatialBlend = 1f; // 3D ses

            // 3. EN ÖNEMLİ KISIM: Ana objenin Mixer Grubunu kopyala
            // (BoneProjectile üzerindeki AudioSource'un output'u neyse bu da o olur)
            if (audioSource != null && audioSource.outputAudioMixerGroup != null)
            {
                tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
            }

            // 4. Çal ve ses bitince objeyi yok et
            tempSource.Play();
            Destroy(tempAudioObj, hitSfx.length);
        }
        // --- DÜZELTİLEN KISIM BİTİŞ ---

        // Hasar işlemleri (Mevcut mantığın)
        if (target != null)
        {
            EnemyHealth enemyHealth = target.GetCurrentComponentInParents<EnemyHealth>();
            // Not: Burada enemyHealth.TakeDamage(damage) gibi bir kod çağırmayı unutma
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

// Yardımcı Extension
public static class ComponentExtensions
{
    public static T GetCurrentComponentInParents<T>(this Component comp) where T : Component
    {
        T c = comp.GetComponent<T>();
        if (c != null) return c;
        return comp.GetComponentInParent<T>();
    }
}