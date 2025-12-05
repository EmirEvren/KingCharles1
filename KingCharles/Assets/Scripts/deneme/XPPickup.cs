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
    public string playerTag = "Animal";   // Senin character tag'in

    [Header("Sesler")]
    public AudioClip magnetSfx;       // Mıknatıs alanına girince bir kere çalsın
    public AudioClip pickupSfx;       // XP toplandığında çalsın
    [Range(0f, 1f)]
    public float sfxVolume = 1f;      // Ses şiddeti

    private static Transform player;
    private static PlayerXP playerXP;

    private AudioSource audioSource;  // Mixer grubunu almak için referans
    private bool magnetSoundPlayed = false;

    private void Awake()
    {
        // 1. AudioSource referansını ayarla (Mixer için gerekli)
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; // 3D ses
        }

        // 2. Player & PlayerXP referansını tek seferde al
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
            if (!magnetSoundPlayed)
            {
                PlayMixerSound(magnetSfx, "TempMagnetSFX");
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
                PlayMixerSound(pickupSfx, "TempPickupSFX");

                Destroy(gameObject);
            }
        }
    }

    // --- SES DÜZELTMESİ İÇİN YARDIMCI FONKSİYON ---
    private void PlayMixerSound(AudioClip clip, string tempObjName)
    {
        if (clip == null) return;

        // 1. Geçici obje oluştur
        GameObject tempObj = new GameObject(tempObjName);
        tempObj.transform.position = transform.position;

        // 2. AudioSource ekle
        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = sfxVolume;
        tempSource.spatialBlend = 1f; // 3D ses

        // 3. KRİTİK: Ana objenin Mixer Grubunu kopyala
        if (audioSource != null && audioSource.outputAudioMixerGroup != null)
        {
            tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        }

        // 4. Çal ve yok et
        tempSource.Play();
        Destroy(tempObj, clip.length);
    }
}