using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro; 
using UnityEngine.Localization; // <-- EKLENDİ

[RequireComponent(typeof(AudioSource))]
public class MascotController : MonoBehaviour, IPointerClickHandler
{
    [Header("--- GÖRSEL AYARLAR ---")]
    public float breatheSpeed = 2f;
    public float breatheAmount = 0.05f;
    public float punchStrength = 0.2f;
    public float punchSpeed = 15f;
    public float shakeAmount = 5f;

    [Header("--- SES AYARLARI ---")]
    public AudioClip[] barkSounds; 
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.9f, maxPitch = 1.1f; 

    [Header("--- PARTİKÜL (KALP/YILDIZ) ---")]
    public ParticleSystem loveParticles; 

    [Header("--- KONUŞMA SİSTEMİ ---")]
    public GameObject speechBubble; 
    public TextMeshProUGUI bubbleText; 
    public float messageDuration = 2f; 
    
    // --- DEĞİŞİKLİK BURADA: ARTIK STRING DEĞİL LOCALIZEDSTRING ---
    [Header("Localization Mesajları (Inspector'dan Seç)")]
    public List<LocalizedString> localizedMessages; 
    // -------------------------------------------------------------

    private Vector3 originalScale;
    private bool isPunched = false;
    private AudioSource audioSource;
    private Coroutine bubbleCoroutine;
    private Quaternion originalRotation;

    void Start()
    {
        originalScale = transform.localScale;
        originalRotation = transform.localRotation;
        audioSource = GetComponent<AudioSource>();

        if(speechBubble != null) speechBubble.SetActive(false);
    }

    void Update()
    {
        if (!isPunched)
        {
            float cycle = Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            transform.localScale = originalScale + new Vector3(cycle, -cycle, 0);
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, Time.deltaTime * 5f);
        }
        else
        {
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * punchSpeed);
            if (Vector3.Distance(transform.localScale, originalScale) < 0.01f)
            {
                isPunched = false;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isPunched = true;

        transform.localScale = originalScale * (1f + punchStrength);

        float randomZ = Random.Range(-shakeAmount, shakeAmount);
        transform.localRotation = Quaternion.Euler(0, 0, randomZ);

        PlayRandomBark();

        if (loveParticles != null) loveParticles.Play();

        ShowRandomMessage();

        Debug.Log("🐶 Maskot Mutlu!");
    }

    private void PlayRandomBark()
    {
        if (barkSounds.Length > 0 && audioSource != null)
        {
            AudioClip clip = barkSounds[Random.Range(0, barkSounds.Length)];
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.PlayOneShot(clip);
        }
    }

    private void ShowRandomMessage()
    {
        // Liste boşsa hata vermesin diye kontrol
        if (speechBubble == null || bubbleText == null || localizedMessages == null || localizedMessages.Count == 0) return;

        // 1. Rastgele bir Localization Key seç
        LocalizedString randomKey = localizedMessages[Random.Range(0, localizedMessages.Count)];

        // 2. O Key'in o anki dildeki karşılığını al ve yazdır
        bubbleText.text = randomKey.GetLocalizedString(); 

        if (bubbleCoroutine != null) StopCoroutine(bubbleCoroutine);
        bubbleCoroutine = StartCoroutine(HideBubbleRoutine());
    }

    IEnumerator HideBubbleRoutine()
    {
        speechBubble.SetActive(true);
        yield return new WaitForSeconds(messageDuration);
        speechBubble.SetActive(false);
    }
}