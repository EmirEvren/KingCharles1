using UnityEngine;

public class GlobalXPMagnet : MonoBehaviour
{
    public static GlobalXPMagnet Instance;

    [Header("Defaults")]
    public float defaultDuration = 10f;

    private float endTime = -1f;

    public bool IsActive => Time.time < endTime;
    public float Remaining => Mathf.Max(0f, endTime - Time.time);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Ýstersen persist:
        // DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Mýknatýsý baþlatýr / resetler. (Topladýkça tekrar 10 saniye olur)
    /// </summary>
    public void Activate(float duration)
    {
        endTime = Time.time + Mathf.Max(0.01f, duration);
    }

    // Sahneye koymayý unutursan diye “auto-create” kolaylýðý:
    public static void ActivateGlobal(float duration = 10f)
    {
        if (Instance == null)
        {
            var go = new GameObject("GlobalXPMagnet");
            Instance = go.AddComponent<GlobalXPMagnet>();
        }
        Instance.Activate(duration);
    }
}