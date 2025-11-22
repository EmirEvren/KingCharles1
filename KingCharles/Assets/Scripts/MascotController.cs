using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
using TMPro; // TextMeshPro kütüphanesini unutma

[RequireComponent(typeof(AudioSource))] // Otomatik AudioSource ekler
public class MascotController : MonoBehaviour, IPointerClickHandler
{
    [Header("--- GÖRSEL AYARLAR ---")]
    public float breatheSpeed = 2f;
    public float breatheAmount = 0.05f;
    public float punchStrength = 0.2f;
    public float punchSpeed = 15f;
    public float shakeAmount = 5f; // Tıklayınca kaç derece dönsün?

    [Header("--- SES AYARLARI ---")]
    public AudioClip[] barkSounds; // Buraya 3-4 farklı havlama sesi at
    [Range(0.8f, 1.2f)]
    public float minPitch = 0.9f, maxPitch = 1.1f; // Ses tonu çeşitliliği

    [Header("--- PARTİKÜL (KALP/YILDIZ) ---")]
    public ParticleSystem loveParticles; // UI Particle System veya World Space

    [Header("--- KONUŞMA SİSTEMİ ---")]
    public GameObject speechBubble; // Balon objesi (Image)
    public TextMeshProUGUI bubbleText; // Balonun içindeki yazı
    public float messageDuration = 2f; // Balon ne kadar ekranda kalsın?
    
    [TextArea]
    public List<string> messages = new List<string>() 
    { 
        "Woof!", 
        "Hadi Oynayalım!", 
        "Kral Charles Emrediyor!", 
        "Mama saati mi?", 
        "Sen bir Alphasın!", 
        "🐶💖" 
    };

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

        // Başlangıçta balonu gizle
        if(speechBubble != null) speechBubble.SetActive(false);
    }

    void Update()
    {
        if (!isPunched)
        {
            // NEFES ALMA (Idle)
            float cycle = Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            transform.localScale = originalScale + new Vector3(cycle, -cycle, 0);
            
            // Rotasyonu düzelt
            transform.localRotation = Quaternion.Lerp(transform.localRotation, originalRotation, Time.deltaTime * 5f);
        }
        else
        {
            // PUNCH GERİ DÖNÜŞ
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * punchSpeed);
            
            // Normale döndüyse
            if (Vector3.Distance(transform.localScale, originalScale) < 0.01f)
            {
                isPunched = false;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isPunched = true;

        // 1. EFEKT: Büyüme (Punch)
        transform.localScale = originalScale * (1f + punchStrength);

        // 2. EFEKT: Sallanma (Shake) - Z ekseninde rastgele dönüş
        float randomZ = Random.Range(-shakeAmount, shakeAmount);
        transform.localRotation = Quaternion.Euler(0, 0, randomZ);

        // 3. SES: Rastgele ve Tonlu Çalma
        PlayRandomBark();

        // 4. PARTİKÜL: Kalp saçma
        if (loveParticles != null) loveParticles.Play();

        // 5. MESAJ: Rastgele konuşma
        ShowRandomMessage();

        Debug.Log("🐶 Maskot Mutlu!");
    }

    private void PlayRandomBark()
    {
        if (barkSounds.Length > 0 && audioSource != null)
        {
            // Rastgele bir ses seç
            AudioClip clip = barkSounds[Random.Range(0, barkSounds.Length)];
            
            // Sese çeşitlilik kat (Pitch Shifting)
            // Bu sayede 1 ses dosyasından 10 farklı sesmiş gibi etki alırsın
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            
            audioSource.PlayOneShot(clip);
        }
    }

    private void ShowRandomMessage()
    {
        if (speechBubble == null || bubbleText == null || messages.Count == 0) return;

        // Rastgele mesaj seç
        string msg = messages[Random.Range(0, messages.Count)];
        bubbleText.text = msg;

        // Eğer zaten bir balon açıksa süresini sıfırla, değilse yeni başlat
        if (bubbleCoroutine != null) StopCoroutine(bubbleCoroutine);
        bubbleCoroutine = StartCoroutine(HideBubbleRoutine());
    }

    IEnumerator HideBubbleRoutine()
    {
        speechBubble.SetActive(true);
        
        // Balonun "Pop" diye açılması için küçük bir animasyon eklenebilir buraya
        
        yield return new WaitForSeconds(messageDuration);
        
        speechBubble.SetActive(false);
    }
}