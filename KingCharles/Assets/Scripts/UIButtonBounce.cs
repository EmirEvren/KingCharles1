using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonBounce : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Settings")]
    public float hoverScale = 1.1f;
    public float clickScale = 0.9f;
    public float speed = 15f;

    private Vector3 originalScale;
    private Vector3 targetScale;

    void Start()
    {
        originalScale = transform.localScale;
        targetScale = originalScale;
    }

    void Update()
    {
        // Yumuşak geçiş (Lerp) - Unity 6 ile gayet performanslıdır
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * speed);
    }

    // Mouse üzerine gelince
    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale;
        // İsteğe bağlı: Hover sesi burada çalınabilir
    }

    // Mouse ayrılınca
    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = originalScale;
    }

    // Tıklayınca (Basılı tutarken)
    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = originalScale * clickScale;
    }

    // Tıklama bitince
    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = originalScale * hoverScale; // Tekrar hover haline dön
    }
}