using UnityEngine;
using MalbersAnimations;   // Malbers Stats için

[RequireComponent(typeof(Rigidbody))]
public class SimpleEnemyFollow : MonoBehaviour
{
    [Header("Hedef")]
    public static Transform Player; // GLOBAL player referansı, tüm düşmanlar kullanacak

    [Header("Hareket")]
    public float moveSpeed = 3.5f;
    public float aiInterval = 0.06f;    // AI tick rate
    public float stopDistance = 1.5f;   // Bu mesafeye kadar gelir, sonra DURUR
    public float attackDistance = 2.0f; // Bu mesafedeyken saldırı animasyonu oynatılır

    private float aiTimer;
    private Vector3 moveDir;    // SADECE XZ yönü (y = 0 tutuluyor)

    [Header("Rotation")]
    public float turnSpeed = 720f; // Derece/sn - her zaman oyuncuya bakma hızı

    [Header("Saldırı")]
    public float attackCooldown = 1.5f; // Saldırılar arasındaki süre
    private float nextAttackTime = 0f;

    [Header("Hasar")]
    public float damage = 10f;      // Enemy'nin vereceği hasar (Inspector'dan ayarlayacaksın)
    public StatID healthStatID;     // Health StatID asset'ini buraya sürükle (Malbers "Health")

    private Stats playerStats;      // Oyuncunun Stats component'i

    [Header("Animasyon")]
    public Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");

    private bool isWalking = false;

    // FİZİK
    private Rigidbody rb;

    [Header("Zemin Algılama")]
    public float groundRayDistance = 2f; // Alttaki zemini ararken kullanılacak mesafe

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Devrilmesin diye rotasyonu kilitle
        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        // Gravity kullansın
        rb.useGravity = true;
        rb.isKinematic = false;   // KESİNLİKLE kinematic olmasın
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // GLOBAL Player referansını tek seferde al
        if (Player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Animal"); // Oyuncu tag'in "Animal"
            if (playerObj != null)
                Player = playerObj.transform;
        }

        // Player'ın Stats component'ini bul
        if (Player != null)
        {
            playerStats = Player.GetComponent<Stats>();
            if (playerStats == null)
                playerStats = Player.GetComponentInParent<Stats>();
        }

        aiTimer = Random.Range(0f, aiInterval); // İşlemciyi yormamak için rastgele offset
    }

    private void Update()
    {
        aiTimer -= Time.deltaTime;

        // AI mantığı belirli aralıklarla çalışır
        if (aiTimer <= 0f)
        {
            aiTimer = aiInterval;
            DoAI();
        }
    }

    // FİZİKSEL HAREKET → FixedUpdate
    private void FixedUpdate()
    {
        // Y eksenine direkt dokunmuyoruz; sadece XZ hızını ayarlıyoruz
        Vector3 currentVel = rb.linearVelocity;
        Vector3 horizontalVel = moveDir * moveSpeed; // XZ hareket

        rb.linearVelocity = new Vector3(
            horizontalVel.x,
            currentVel.y,   // düşme/çıkma gravity’den gelsin
            horizontalVel.z
        );

        // Altındaki collidere göre Y'yi düzelt
        GroundStick();

        // Her zaman oyuncuya bak (physics uyumlu)
        FacePlayerPhysics();
    }

    private void FacePlayerPhysics()
    {
        if (Player == null) return;

        Vector3 dir = Player.position - rb.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);

        // Çarpışmadan gelen "spin"i kes
        rb.angularVelocity = Vector3.zero;

        // Physics uyumlu rotasyon
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
    }

    /// <summary>
    /// Altındaki zemine karakteri yapıştır.
    /// Climb ise (rampa) dokunma, diğer her şeyi zemin kabul et (Terrain dahil).
    /// </summary>
    private void GroundStick()
    {
        // Ray'i biraz yukarıdan başlat (tam ayak tabanından başlatma ki zeminin içine girmesin)
        Vector3 origin = rb.position + Vector3.up * 0.5f;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundRayDistance))
        {
            // Rampa ise (tag: Climb) → ELLEME
            if (hit.collider.CompareTag("Climb"))
            {
                return;
            }

            // Diğer her collider (TerrainCollider, MeshCollider, vb.) zemin say:
            Vector3 pos = rb.position;
            pos.y = hit.point.y;

            // Y eksenindeki hızı sıfırlayıp pozisyonu zemine çekiyoruz
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.MovePosition(pos);
        }
    }

    private void DoAI()
    {
        if (Player == null)
        {
            moveDir = Vector3.zero;
            SetWalking(false);
            return;
        }

        Vector3 dir = Player.position - transform.position;
        dir.y = 0f; // Y yönünü yok say, sadece XZ'de takip et

        float dist = dir.magnitude;

        // Uzaksa → YÜRÜ
        if (dist > stopDistance)
        {
            moveDir = dir.normalized;   // XZ yönü
            SetWalking(true);
        }
        else
        {
            // Stop mesafesine geldiyse → DUR
            moveDir = Vector3.zero;
            SetWalking(false);

            // Saldırı mesafesi içindeyse ve cooldown bittiyse → SALDIR
            if (dist <= attackDistance && Time.time >= nextAttackTime)
            {
                if (animator != null)
                {
                    animator.SetTrigger(AttackHash); // "Attack" trigger'ını tetikle
                }

                ApplyDamage(); // CAN AZALT

                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator == null) return;
        if (isWalking == walking) return; // Gereksiz set etmeyelim

        isWalking = walking;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    /// <summary>
    /// Oyuncunun Health stat'ini düşür.
    /// </summary>
    private void ApplyDamage()
    {
        if (playerStats == null) return;
        if (healthStatID == null) return;

        // Malbers Stats sistemiyle Health düşürme
        playerStats.Stat_ModifyValue(healthStatID, -damage);
    }
}
