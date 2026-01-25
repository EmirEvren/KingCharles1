using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DogHouseUpgradeStation : MonoBehaviour
{
    [Header("Interaction")]
    public string playerTag = "Animal";
    public KeyCode interactKey = KeyCode.E;
    public float holdDuration = 2f;

    [Header("UI")]
    public Slider holdSlider;              // value 0..1
    public DogHouseUpgradeUI upgradeUI;    // 3 buton paneli

    [Header("Seçim Açılınca Durdurulacak Scriptler (Opsiyonel)")]
    public MonoBehaviour[] scriptsToDisableWhileChoosing;

    [Header("Destroy")]
    [Tooltip("Boş bırakırsan bu GameObject'i siler. Doghouse kökünü silmek istiyorsan buraya root objeyi ver.")]
    public GameObject destroyTarget;

    private bool inRange = false;
    private bool usedThisStay = false;
    private float holdTimer = 0f;

    // 1 kere açıldı mı? (Exploit kilidi)
    private bool usedEver = false;

    // UI kapandıktan sonra silinsin
    private bool destroyAfterClose = false;

    private static readonly string[] RarityNames = { "Common", "Uncommon", "Rare", "Epic", "Legendary" };

    private static readonly int[] XP_TIERS = { 1, 2, 3, 4, 5 };
    private static readonly int[] GOLD_TIERS = { 1, 2, 3, 4, 5 };
    private static readonly int[] DMG_TIERS = { 5, 10, 15, 20, 25 };
    private static readonly float[] SPEED_TIERS = { 0.1f, 0.15f, 0.2f, 0.25f, 0.5f };
    private static readonly int[] LUCK_TIERS = { 1, 3, 5, 10, 20 };
    private static readonly int[] MAXHP_TIERS = { 10, 25, 50, 100, 250 };

    private void Awake()
    {
        // Start yerine Awake: event kaçırma riskini azaltır
        if (upgradeUI != null)
            upgradeUI.onClosed += OnUpgradeClosed;
    }

    private void OnDestroy()
    {
        // Memory leak / ghost callback önlemi
        if (upgradeUI != null)
            upgradeUI.onClosed -= OnUpgradeClosed;
    }

    private void Start()
    {
        if (holdSlider != null)
        {
            holdSlider.gameObject.SetActive(false);
            holdSlider.minValue = 0f;
            holdSlider.maxValue = 1f;
            holdSlider.value = 0f;
        }
    }

    private void Update()
    {
        // Eğer bir kere açıldıysa ve kapanmışsa (hangi yolla kapanırsa kapansın) sil
        if (destroyAfterClose)
        {
            if (upgradeUI == null || !upgradeUI.IsOpen)
            {
                DestroySelf();
                return;
            }
        }

        if (!inRange) return;
        if (usedEver) return; // Exploit kilidi
        if (upgradeUI != null && upgradeUI.IsOpen) return;
        if (usedThisStay) return;

        if (Input.GetKey(interactKey))
        {
            holdTimer += Time.deltaTime;

            if (holdSlider != null)
            {
                if (!holdSlider.gameObject.activeSelf) holdSlider.gameObject.SetActive(true);
                holdSlider.value = Mathf.Clamp01(holdTimer / holdDuration);
            }

            if (holdTimer >= holdDuration)
            {
                holdTimer = 0f;
                if (holdSlider != null)
                {
                    holdSlider.value = 0f;
                    holdSlider.gameObject.SetActive(false);
                }

                OpenUpgradeChoices();
                usedThisStay = true;
            }
        }
        else
        {
            holdTimer = 0f;
            if (holdSlider != null && holdSlider.gameObject.activeSelf)
            {
                holdSlider.value = 0f;
                holdSlider.gameObject.SetActive(false);
            }
        }
    }

    private void OpenUpgradeChoices()
    {
        if (upgradeUI == null) return;

        // Exploit kilidi: UI açıldı mı artık tekrar açma
        usedEver = true;

        List<DogUpgradeType> pool = new List<DogUpgradeType>
        {
            DogUpgradeType.XPGainRate,
            DogUpgradeType.GoldGainRate,
            DogUpgradeType.GlobalWeaponDamage,
            DogUpgradeType.MoveSpeed,
            DogUpgradeType.Luck,
            DogUpgradeType.MaxHealth
        };

        Shuffle(pool);

        DogHouseUpgradeOption o1 = RollOption(pool[0]);
        DogHouseUpgradeOption o2 = RollOption(pool[1]);
        DogHouseUpgradeOption o3 = RollOption(pool[2]);

        Time.timeScale = 0f;
        SetExtraScriptsEnabled(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        upgradeUI.Open(o1, o2, o3);

        // UI kapanınca silinecek. (OnClosed kaçarsa Update fallback yakalar)
        destroyAfterClose = true;
    }

    private DogHouseUpgradeOption RollOption(DogUpgradeType type)
    {
        int luck = PlayerLuck.Instance != null ? PlayerLuck.Instance.luckLevel : 0;
        int tier = RollTierIndexByLuck(luck); // 0..4

        DogHouseUpgradeOption opt = new DogHouseUpgradeOption
        {
            type = type,
            tierIndex = tier,
            rarityName = RarityNames[tier],
            intValue = 0,
            floatValue = 0f
        };

        switch (type)
        {
            case DogUpgradeType.XPGainRate: opt.intValue = XP_TIERS[tier]; break;
            case DogUpgradeType.GoldGainRate: opt.intValue = GOLD_TIERS[tier]; break;
            case DogUpgradeType.GlobalWeaponDamage: opt.intValue = DMG_TIERS[tier]; break;
            case DogUpgradeType.MoveSpeed: opt.floatValue = SPEED_TIERS[tier]; break;
            case DogUpgradeType.Luck: opt.intValue = LUCK_TIERS[tier]; break;
            case DogUpgradeType.MaxHealth: opt.intValue = MAXHP_TIERS[tier]; break;
        }

        return opt;
    }

    private int RollTierIndexByLuck(int luckLevel)
    {
        float norm = 1f - Mathf.Exp(-luckLevel / 15f);
        float pLegend = Mathf.Lerp(0.02f, 0.90f, norm);

        float remaining = 1f - pLegend;

        float[] w0 = { 0.55f, 0.25f, 0.13f, 0.05f };
        float[] w1 = { 0.10f, 0.15f, 0.25f, 0.50f };

        float[] w = new float[4];
        float sum = 0f;
        for (int i = 0; i < 4; i++)
        {
            w[i] = Mathf.Lerp(w0[i], w1[i], norm);
            sum += w[i];
        }

        for (int i = 0; i < 4; i++)
            w[i] = (w[i] / sum) * remaining;

        float r = Random.value;

        if (r < pLegend) return 4;
        r -= pLegend;

        if (r < w[3]) return 3;
        r -= w[3];

        if (r < w[2]) return 2;
        r -= w[2];

        if (r < w[1]) return 1;

        return 0;
    }

    private void OnUpgradeClosed()
    {
        Time.timeScale = 1f;
        SetExtraScriptsEnabled(true);
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // Normal yol: seçimden sonra Close() geldi → burada sil
        if (destroyAfterClose)
            DestroySelf();
    }

    private void DestroySelf()
    {
        // Slider vs açık kalmasın
        if (holdSlider != null)
        {
            holdSlider.value = 0f;
            holdSlider.gameObject.SetActive(false);
        }

        // Hedef verildiyse onu, yoksa kendini sil
        GameObject target = destroyTarget != null ? destroyTarget : gameObject;
        Destroy(target);
    }

    private void SetExtraScriptsEnabled(bool enabled)
    {
        if (scriptsToDisableWhileChoosing == null) return;
        foreach (var s in scriptsToDisableWhileChoosing)
        {
            if (s != null) s.enabled = enabled;
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = true;
        usedThisStay = false;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        inRange = false;
        usedThisStay = false;
        holdTimer = 0f;

        if (holdSlider != null)
        {
            holdSlider.value = 0f;
            holdSlider.gameObject.SetActive(false);
        }
    }
}
