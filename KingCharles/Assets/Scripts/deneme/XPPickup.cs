using UnityEngine;

public class XPPickup : MonoBehaviour
{
    [Header("XP Ayarları")]
    public int xpAmount = 5;          // Bu kutunun verdiği XP
    public float lifeTime = 15f;      // Çok uzaklara saçılırsa yok olsun

    [Header("Mıknatıs Ayarları")]
    public float magnetRadius = 6f;   // Bu mesafeye girince oyuncuya çekilmeye başlar
    public float flySpeed = 15f;      // Oyuncuya doğru uçma hızı
    public float collectDistance = 1.2f; // Bu kadar yakına gelince toplanmış sayılır

    [Header("Player Tag")]
    public string playerTag = "Animal";   // Senin character tag'in neyse (Animal demiştin)

    [Header("Sesler")]
    public AudioClip magnetSfx;       // Mıknatıs alanına girince bir kere çalsın
    public AudioClip pickupSfx;       // XP toplandığında çalsın
    public float sfxVolume = 1f;      // Ses şiddeti

    private static Transform player;
    private static PlayerXP playerXP;

    private bool magnetSoundPlayed = false;

    private void Awake()
    {
        // Player & PlayerXP referansını tek seferde al
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
            if (pObj != null)
            {
                player = pObj.transform;
                playerXP = player.GetComponent<PlayerXP>();
                if (playerXP == null)
                    Debug.LogWarning("[XPPickup] PlayerXP component bulunamadı, XP verilemeyecek!");
            }
        }
    }

    private void Start()
    {
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    private void Update()
    {
        if (player == null) return;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f; // Y farkını önemsiz say

        float sqrDist = toPlayer.sqrMagnitude;
        float sqrMagnet = magnetRadius * magnetRadius;
        float sqrCollect = collectDistance * collectDistance;

        // Mıknatıs alanına girdiyse → oyuncuya doğru uç
        if (sqrDist <= sqrMagnet)
        {
            // Mıknatıs sesini sadece ilk kez girdiğinde çal
            if (!magnetSoundPlayed && magnetSfx != null)
            {
                AudioSource.PlayClipAtPoint(magnetSfx, transform.position, sfxVolume);
                magnetSoundPlayed = true;
            }

            Vector3 dir = toPlayer.normalized;
            transform.position += dir * flySpeed * Time.deltaTime;

            // Toplama mesafesine girdiyse → XP ver ve yok ol
            if (sqrDist <= sqrCollect)
            {
                if (playerXP != null)
                {
                    playerXP.AddXP(xpAmount);
                }

                // Pickup sesi
                if (pickupSfx != null)
                {
                    AudioSource.PlayClipAtPoint(pickupSfx, transform.position, sfxVolume);
                }

                Destroy(gameObject);
            }
        }
    }
}
