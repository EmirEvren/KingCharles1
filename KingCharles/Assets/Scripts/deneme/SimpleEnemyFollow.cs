using UnityEngine;
using MalbersAnimations;   // Malbers Stats için

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
    private Vector3 moveDir;

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

        // Hareket her frame çizgisel olarak uygula (smooth olsun diye)
        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
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
        dir.y = 0f;

        float dist = dir.magnitude;

        // Her durumda oyuncuya doğru bak
        if (dir.sqrMagnitude > 0.001f)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
        }

        // Uzaksa → YÜRÜ
        if (dist > stopDistance)
        {
            moveDir = dir.normalized;
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
