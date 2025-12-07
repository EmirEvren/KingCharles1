using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    [Header("Hareket / Ömür")]
    public float moveSpeed = 1.5f;
    public float lifeTime = 0.7f;
    public Vector3 moveDirection = new Vector3(0f, 1f, 0f);

    private TextMeshPro textMesh;
    private Color textColor;
    private float timer;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            Debug.LogError("[DamagePopup] TextMeshPro component'i yok! DamagePopup prefab'ine TextMeshPro ekle.");
        }
    }

    /// <summary>
    /// DamagePopupManager burayı çağırıyor.
    /// </summary>
    public void Setup(float damageAmount, Color color)
    {
        if (textMesh == null) return;

        // 45.3 → 45 gibi
        textMesh.text = Mathf.RoundToInt(damageAmount).ToString();

        textColor = color;
        textMesh.color = textColor;

        timer = lifeTime;
    }

    private void Update()
    {
        // Yukarı doğru hareket
        transform.position += moveDirection * moveSpeed * Time.deltaTime;

        // Kameraya bak
        Camera cam = Camera.main;
        if (cam != null)
        {
            transform.forward = cam.transform.forward;
        }

        // Ömür / fade
        timer -= Time.deltaTime;
        if (timer < lifeTime * 0.5f)
        {
            // Son yarısında yavaş yavaş şeffaflık
            float fadeSpeed = 1f / (lifeTime * 0.5f);
            textColor.a -= fadeSpeed * Time.deltaTime;
            textMesh.color = textColor;
        }

        if (timer <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
