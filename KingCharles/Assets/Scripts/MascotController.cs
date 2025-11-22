using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems; // Tıklama algılamak için

public class MascotController : MonoBehaviour, IPointerClickHandler
{
    [Header("Nefes Alma Ayarları")]
    public float breatheSpeed = 2f;      // Ne kadar hızlı nefes alsın?
    public float breatheAmount = 0.05f;  // Ne kadar şişip insin?

    [Header("Tıklama Ayarları")]
    public float punchStrength = 0.2f;   // Tıklayınca ne kadar büyüsün?
    public float punchSpeed = 10f;       // Eski haline dönme hızı

    private Vector3 originalScale;
    private Vector3 targetScale;
    private bool isPunched = false;

    // Konuşma balonu referansı (Opsiyonel)
    public GameObject speechBubble;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Eğer tıklanmadıysa sakince nefes alıp ver (Sinüs dalgası)
        if (!isPunched)
        {
            float cycle = Mathf.Sin(Time.time * breatheSpeed) * breatheAmount;
            // Y ekseninde (boyuna) uzarken X ekseninde (enine) daralırsa "elastik" görünür
            transform.localScale = originalScale + new Vector3(cycle, -cycle, 0); 
        }
        else
        {
            // Tıklandıysa, yavaşça orijinal boyuta (nefes alma döngüsüne) dön
            transform.localScale = Vector3.Lerp(transform.localScale, originalScale, Time.deltaTime * punchSpeed);
            
            // Yeterince küçüldüyse tekrar nefes alma moduna geç
            if (Vector3.Distance(transform.localScale, originalScale) < 0.01f)
            {
                isPunched = false;
            }
        }
    }

    // Maskota tıklanınca çalışır
    public void OnPointerClick(PointerEventData eventData)
    {
        // Anlık olarak büyüt (Punch efekti)
        transform.localScale = originalScale * (1f + punchStrength);
        isPunched = true;

        // Rastgele havlama sesi çaldırabilirsin (AudioSource varsa)
        // GetComponent<AudioSource>().Play();

        Debug.Log("🐶 WOOF! Beni gıdıkladın!");
    }
}