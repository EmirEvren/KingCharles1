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
        // --- ÇARPMA SESİ ---
        if (hitSfx != null)
        {
            GameObject tempAudioObj = new GameObject("TempBoneHitSFX");
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

        // --- HASAR (upgrade'li) ---
        if (target != null)
        {
            EnemyHealth enemyHealth = target.GetCurrentComponentInParents<EnemyHealth>();
            if (enemyHealth != null)
            {
                float finalDamage = damage;

                if (WeaponChoiceManager.Instance != null)
                {
                    finalDamage = WeaponChoiceManager.Instance.GetModifiedDamage(WeaponType.Bone, damage);
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
