using UnityEngine;

public class GlobalIceBreath : MonoBehaviour
{
    public static GlobalIceBreath Instance;

    [Header("Player")]
    public string playerTag = "Animal";
    public Transform mouthPoint; // Player'da MouthPoint (empty child) ver

    [Header("Ice Prefab")]
    public GameObject iceBreathPrefab;

    [Header("Damage")]
    public float damage = 500f;

    [Header("Duration")]
    public float durationSeconds = 10f; // ✅ 10 saniye aktif kalacak

    [Header("Tick")]
    public float tickInterval = 1f; // ✅ her saniye 500 vuracak

    // (Eski alanı koruyorum, çıkartmıyorum)
    [Header("Spawn")]
    public float autoDestroySeconds = 3f; // (Artık destroy etmiyoruz ama değişken kalsın)

    private Transform player;

    // ✅ Runtime state
    private GameObject activeFx;
    private float remainingTime = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Update()
    {
        if (remainingTime <= 0f) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            StopEffect();
        }
    }

    private void EnsurePlayer()
    {
        if (player != null && mouthPoint != null) return;

        GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
        if (pObj == null) return;

        player = pObj.transform;

        // mouthPoint inspector’dan verilmediyse bulmaya çalış (opsiyonel)
        if (mouthPoint == null)
        {
            // Player altında "MouthPoint" diye bir empty açarsan otomatik bulur:
            var t = player.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] != null && t[i].name == "MouthPoint")
                {
                    mouthPoint = t[i];
                    break;
                }
            }
        }
    }

    public void Trigger()
    {
        EnsurePlayer();

        if (iceBreathPrefab == null)
        {
            Debug.LogWarning("[GlobalIceBreath] iceBreathPrefab yok!");
            return;
        }

        if (mouthPoint == null)
        {
            Debug.LogWarning("[GlobalIceBreath] mouthPoint yok! Player içine MouthPoint empty child ekle ve ver.");
            return;
        }

        // ✅ Süreyi resetle (her yeni gem 10 saniyeye çeker)
        remainingTime = Mathf.Max(0.01f, durationSeconds);

        // ✅ FX yoksa spawn et, varsa tekrar aç
        if (activeFx == null)
        {
            // mouthPoint child’ı olarak spawn → oyuncu hareket edince beraber gider
            activeFx = Instantiate(iceBreathPrefab, mouthPoint);
            activeFx.transform.localPosition = Vector3.zero;
            activeFx.transform.localRotation = Quaternion.identity;
        }
        else
        {
            // MouthPoint değiştiyse tekrar parentla
            if (activeFx.transform.parent != mouthPoint)
                activeFx.transform.SetParent(mouthPoint, false);

            if (!activeFx.activeSelf)
                activeFx.SetActive(true);

            activeFx.transform.localPosition = Vector3.zero;
            activeFx.transform.localRotation = Quaternion.identity;
        }

        // ✅ damage + tick ayarla (IceBreathDamage yeni halindeyse)
        var dmg = activeFx.GetComponent<IceBreathDamage>();
        if (dmg != null)
        {
            dmg.Setup(damage, tickInterval);
        }
    }

    private void StopEffect()
    {
        if (activeFx != null)
        {
            activeFx.SetActive(false);
        }
    }

    public static void TriggerGlobal()
    {
        if (Instance == null)
        {
            var go = new GameObject("GlobalIceBreath");
            Instance = go.AddComponent<GlobalIceBreath>();
        }

        Instance.Trigger();
    }
}