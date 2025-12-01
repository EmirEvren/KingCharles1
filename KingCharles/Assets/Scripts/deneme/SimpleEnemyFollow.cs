using UnityEngine;

public class SimpleEnemyFollow : MonoBehaviour
{
    [Header("Ayarlar")]
    public static Transform Player; // GLOBAL player referansı, tüm düşmanlar bunu kullanacak
    public float moveSpeed = 3.5f;
    public float aiInterval = 0.06f;   // AI tick rate (Saniyede ~16 kez çalışır)
    private float aiTimer;

    [Header("Animasyon")]
    public Animator animator;
    private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");

    private Vector3 moveDir;
    private bool isWalking = false; // Animasyon kontrolü için

    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // GLOBAL Player referansını tek seferde al
        if (Player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Animal");
            if (playerObj != null)
                Player = playerObj.transform;
        }

        aiTimer = Random.Range(0f, aiInterval); // İşlemciyi yormamak için rastgele başlat
    }

    private void Update()
    {
        aiTimer -= Time.deltaTime;

        // AI Mantığı (Belirli aralıklarla çalışır - Performans dostu)
        if (aiTimer <= 0f)
        {
            aiTimer = aiInterval;
            DoAI();
        }

        // Fiziksel hareket her karede (frame) olmalı ki akıcı görünsün
        if (moveDir.sqrMagnitude > 0.001f)
        {
            transform.position += moveDir * moveSpeed * Time.deltaTime;
        }
    }

    private void DoAI()
    {
        // Eğer GLOBAL Player yoksa bu turu pas geç
        if (Player == null)
        {
            moveDir = Vector3.zero;
            if (animator != null && isWalking)
            {
                animator.SetBool(IsWalkingHash, false);
                isWalking = false;
            }
            return;
        }

        Vector3 dir = Player.position - transform.position;
        dir.y = 0f; // Havaya bakmasını engelle

        float dist = dir.magnitude;

        if (dist > 0.5f) // Çok yaklaşınca dur
        {
            moveDir = dir.normalized;

            // Yumuşak dönme
            if (moveDir != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, Time.deltaTime * 10f);
            }

            // Animasyon sadece değişirse çalışsın
            if (!isWalking && animator != null)
            {
                animator.SetBool(IsWalkingHash, true);
                isWalking = true;
            }
        }
        else
        {
            moveDir = Vector3.zero;

            if (isWalking && animator != null)
            {
                animator.SetBool(IsWalkingHash, false);
                isWalking = false;
            }
        }
    }
}
