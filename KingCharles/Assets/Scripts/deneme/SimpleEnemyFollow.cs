using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

[RequireComponent(typeof(Rigidbody))]
public class SimpleEnemyFollow : MonoBehaviour
{
    [Header("Hedef")]
    public static Transform Player;

    [Header("Hareket")]
    public float moveSpeed = 3.5f;
    public float aiInterval = 0.06f;
    public float stopDistance = 1.5f;
    public float attackDistance = 2.0f;

    private float aiTimer;
    private Vector3 moveDir;

    [Header("Rotation")]
    public float turnSpeed = 720f;

    [Header("Saldırı")]
    public float attackCooldown = 1.5f;
    private float nextAttackTime = 0f;

    [Header("Hasar")]
    public float damage = 10f;
    public StatID healthStatID;

    private Stats playerStats;

    [Header("Animasyon")]
    public Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private bool isWalking = false;

    // FİZİK
    private Rigidbody rb;

    [Header("Zemin Algılama (Slope + StepDown Fix)")]
    [Tooltip("Mümkünse sadece Ground/Terrain layer seç. Enemy layer'ı burada OLMASIN.")]
    public LayerMask groundMask = ~0;

    [Tooltip("Ray başlangıcı: pivotun üstünden başlasın.")]
    public float groundProbeUp = 0.6f;

    [Tooltip("Aşağı ray mesafesi.")]
    public float groundRayDistance = 2.5f;

    [Tooltip("Pivot ile zemin arası bu değerden büyükse grounded sayma (havada yürümeyi keser).")]
    public float groundedTolerance = 0.25f;

    [Tooltip("Yere bastırma kuvveti (engebede zıplama varsa artır).")]
    public float groundStickForce = 35f;

    [Tooltip("Havadayken ekstra aşağı ivme (hover oluyorsa 0 -> 10-30 arası dene).")]
    public float extraGravityInAir = 15f;

    [Tooltip("İstersen 'Climb' tag'li collider'ları zemin sayma.")]
    public bool ignoreClimbTag = false;

    private bool grounded;
    private Vector3 groundNormal = Vector3.up;
    private float groundClearance = 999f; // pivot->zemin mesafesi (yaklaşık)

    private static readonly RaycastHit[] s_groundHits = new RaycastHit[12];

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.constraints = RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;

        rb.useGravity = true;
        rb.isKinematic = false;
    }

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (Player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Animal");
            if (playerObj != null)
                Player = playerObj.transform;
        }

        if (Player != null)
        {
            playerStats = Player.GetComponent<Stats>();
            if (playerStats == null)
                playerStats = Player.GetComponentInParent<Stats>();
        }

        float difficultyMul = GetDifficultyMultiplier();
        if (difficultyMul > 0f)
            damage *= difficultyMul;

        aiTimer = Random.Range(0f, aiInterval);
    }

    private void Update()
    {
        aiTimer -= Time.deltaTime;
        if (aiTimer <= 0f)
        {
            aiTimer = aiInterval;
            DoAI();
        }
    }

    private void FixedUpdate()
    {
        UpdateGrounding();

        Vector3 currentVel = rb.linearVelocity;

        // 1) Yönü zemine projekte et (yokuş çık/iniş)
        Vector3 desiredDir = moveDir;

        if (grounded && desiredDir.sqrMagnitude > 0.0001f)
        {
            Vector3 onSlope = Vector3.ProjectOnPlane(desiredDir, groundNormal);
            if (onSlope.sqrMagnitude > 0.0001f)
                desiredDir = onSlope.normalized;
        }

        // 2) Hedef hız
        Vector3 targetVel;

        if (desiredDir.sqrMagnitude > 0.0001f)
        {
            targetVel = desiredDir * moveSpeed;

            // Havada ise Y'yi gravity yönetsin
            if (!grounded)
                targetVel.y = currentVel.y;
        }
        else
        {
            // Duruyorsa XZ=0
            targetVel = new Vector3(0f, currentVel.y, 0f);

            // Zemindeyken dikey hız istemiyoruz (micro zıplama vs.)
            if (grounded)
                targetVel.y = 0f;
        }

        rb.linearVelocity = targetVel;

        // 3) Zemindeyken bastırma
        if (grounded)
        {
            rb.AddForce(-groundNormal * groundStickForce, ForceMode.Acceleration);
        }
        else
        {
            // Hover olmasın diye ekstra aşağı ivme (gravity’ye ek)
            if (extraGravityInAir > 0f)
                rb.AddForce(Vector3.down * extraGravityInAir, ForceMode.Acceleration);
        }

        FacePlayerPhysics();
    }

    private void UpdateGrounding()
    {
        grounded = false;
        groundNormal = Vector3.up;
        groundClearance = 999f;

        Vector3 origin = rb.position + Vector3.up * groundProbeUp;

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            Vector3.down,
            s_groundHits,
            groundRayDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        if (hitCount <= 0) return;

        float bestDist = float.PositiveInfinity;
        RaycastHit bestHit = default;
        bool found = false;

        for (int i = 0; i < hitCount; i++)
        {
            var h = s_groundHits[i];
            if (h.collider == null) continue;

            // Kendi collider'ını yok say (child dahil)
            if (h.collider.transform.IsChildOf(transform)) continue;

            if (ignoreClimbTag && h.collider.CompareTag("Climb")) continue;

            if (h.distance < bestDist)
            {
                bestDist = h.distance;
                bestHit = h;
                found = true;
            }
        }

        if (!found) return;

        groundNormal = bestHit.normal;

        // Origin = pivot + up*probeUp olduğundan, pivot->zemin yaklaşık:
        groundClearance = bestHit.distance - groundProbeUp;

        // Asıl kritik: zemin “yakınsa” grounded say
        grounded = (groundClearance <= groundedTolerance);
    }

    private void FacePlayerPhysics()
    {
        if (Player == null) return;

        Vector3 dir = Player.position - rb.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized);
        rb.angularVelocity = Vector3.zero;
        rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRot, turnSpeed * Time.fixedDeltaTime));
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

        if (dist > stopDistance)
        {
            moveDir = dir.normalized;
            SetWalking(true);
        }
        else
        {
            moveDir = Vector3.zero;
            SetWalking(false);

            if (dist <= attackDistance && Time.time >= nextAttackTime)
            {
                if (animator != null)
                    animator.SetTrigger(AttackHash);

                ApplyDamage();
                nextAttackTime = Time.time + attackCooldown;
            }
        }
    }

    private void SetWalking(bool walking)
    {
        if (animator == null) return;
        if (isWalking == walking) return;

        isWalking = walking;
        animator.SetBool(IsWalkingHash, isWalking);
    }

    private void ApplyDamage()
    {
        if (playerStats == null) return;
        if (healthStatID == null) return;

        playerStats.Stat_ModifyValue(healthStatID, -damage);
    }

    private int GetDifficultyMinutes()
    {
        int minutesFromTime = Mathf.FloorToInt(Time.timeSinceLevelLoad / 60f);

        int bonus = 0;
        if (PlayerPermanentUpgrades.Instance != null)
            bonus = Mathf.Max(0, PlayerPermanentUpgrades.Instance.difficultyBonusMinutes);

        return Mathf.Max(0, minutesFromTime + bonus);
    }

    private float GetDifficultyMultiplier()
    {
        int m = GetDifficultyMinutes();
        return Mathf.Pow(1.2f, m);
    }
}
