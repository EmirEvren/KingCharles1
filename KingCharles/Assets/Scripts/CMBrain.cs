using UnityEngine;
using UnityEngine.InputSystem;

public class CMBrain : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;
    public string playerTag = "Player";

    [Header("Follow Settings")]
    public Vector3 offset = new Vector3(10, 15, -10);
    
    // SmoothDamp için bu değer 'zaman' cinsindendir. 
    // 0.1f - 0.2f arası en iyi yumuşaklığı verir.
    public float smoothTime = 0.15f; 

    [Header("Zoom Settings")]
    public float zoomSpeed = 0.05f; // Yeni input sisteminde hassas ayar
    public float minZoom = 5f;
    public float maxZoom = 25f;
    
    private float currentZoom = 10f;
    
    // SmoothDamp referansı için gerekli değişken
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (target == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null) target = playerObj.transform;
        }
    }

    // Fizik takibi için LateUpdate en iyisidir
    void LateUpdate()
    {
        if (target == null) return;
        HandleZoom();
        HandleMovement();
    }

    void HandleMovement()
    {
        // Hedef pozisyonu hesapla
        Vector3 adjustedOffset = offset.normalized * currentZoom; 
        Vector3 desiredPosition = target.position + adjustedOffset;
        
        // ESKİ YÖNTEM (Lerp): Titreme yapabilir
        // Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        
        // YENİ YÖNTEM (SmoothDamp): Titremeyi emer, yağ gibi kayar
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        
        // Kameranın hedefe bakması
        transform.LookAt(target);
    }

    void HandleZoom()
    {
        float scrollInput = 0f;
        if (Mouse.current != null)
        {
            scrollInput = Mouse.current.scroll.ReadValue().y * 0.01f; 
        }
        
        currentZoom -= scrollInput * zoomSpeed;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}