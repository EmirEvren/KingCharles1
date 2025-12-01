using UnityEngine;

public class SimpleEnemyFollow : MonoBehaviour
{
    public Transform target;
    public float moveSpeed = 3.5f;

    [Header("Animasyon")]
    public Animator animator;          // Düþmanýn Animator'u
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private void Awake()
    {
        // Inspector'dan atamazsan otomatik bulsun
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        // Hedef yoksa her frame yeniden dene
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Animal"); // Oyuncu tag'in "Animal" OLMALI
            if (playerObj != null)
                target = playerObj.transform;

            if (target == null)
                return; // Hala bulamadýysa aþaðýya hiç girme
        }

        Vector3 dir = target.position - transform.position;
        dir.y = 0f; // Yukarý-aþaðý dönmesin

        float distance = dir.magnitude;

        if (distance > 0.1f) // Çok yakýnda deðilse yürüsün
        {
            Vector3 moveDir = dir.normalized;

            // Yürüt
            transform.position += moveDir * moveSpeed * Time.deltaTime;

            // Yönünü hedefe çevir
            if (moveDir.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(moveDir);

            // Animasyon: Yürüyüþ açýk
            if (animator != null)
                animator.SetBool(IsWalkingHash, true);
        }
        else
        {
            // Durunca idle
            if (animator != null)
                animator.SetBool(IsWalkingHash, false);
        }
    }
}
