using UnityEngine;

public class GoldPickup : MonoBehaviour
{
    [Header("Altın Ayarları")]
    public int goldValue = 2;
    public float lifeTime = 15f;

    [Header("Mıknatıs Ayarları")]
    public float magnetRadius = 6f;
    public float flySpeed = 15f;
    public float collectDistance = 1.2f;

    [Header("Player Tag")]
    public string playerTag = "Animal";

    [Header("Sesler")]
    public AudioClip magnetSfx;
    public AudioClip pickupSfx;
    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    private static Transform player;
    private AudioSource audioSource;
    private bool magnetSoundPlayed = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }

        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
            if (pObj != null)
            {
                player = pObj.transform;
            }
            else
            {
                Debug.LogWarning("[GoldPickup] Player tag'li obje bulunamadı.");
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
        toPlayer.y = 0f;

        float sqrDist = toPlayer.sqrMagnitude;
        float sqrMagnet = magnetRadius * magnetRadius;
        float sqrCollect = collectDistance * collectDistance;

        if (sqrDist <= sqrMagnet)
        {
            if (!magnetSoundPlayed)
            {
                PlayMixerSound(magnetSfx, "TempGoldMagnetSFX");
                magnetSoundPlayed = true;
            }

            Vector3 dir = toPlayer.normalized;
            transform.position += dir * flySpeed * Time.deltaTime;

            if (sqrDist <= sqrCollect)
            {
                GoldCounterUI.RegisterGold(goldValue);
                PlayMixerSound(pickupSfx, "TempGoldPickupSFX");
                Destroy(gameObject);
            }
        }
    }

    private void PlayMixerSound(AudioClip clip, string tempObjName)
    {
        if (clip == null) return;

        GameObject tempObj = new GameObject(tempObjName);
        tempObj.transform.position = transform.position;

        AudioSource tempSource = tempObj.AddComponent<AudioSource>();
        tempSource.clip = clip;
        tempSource.volume = sfxVolume;
        tempSource.spatialBlend = 1f;

        if (audioSource != null && audioSource.outputAudioMixerGroup != null)
        {
            tempSource.outputAudioMixerGroup = audioSource.outputAudioMixerGroup;
        }

        tempSource.Play();
        Destroy(tempObj, clip.length);
    }
}
