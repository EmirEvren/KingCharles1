using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum WeaponType
{
    Bone,
    Steak,
    Fireball,
    TennisBall
}

public enum UpgradeKind
{
    Count,          // Fazladan mermi
    AttackSpeed,    // Atış hızı çarpanı
    Damage          // Düz +hasar
}

public enum UpgradeRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary
}

[Serializable]
public class WeaponOption
{
    public string weaponName;             // Kartta görünen isim
    public WeaponType type;              // Hangi silah
    public MonoBehaviour shooterScript;  // Player üzerindeki AutoShooter
    public Sprite icon;                  // BUTON görseli için kullanılacak icon
}

[Serializable]
public class WeaponRuntimeStats
{
    public WeaponType type;
    public int extraCount;                      // Fazladan mermi sayısı
    public float damageBonus;                   // Düz +damage
    public float attackSpeedMultiplier = 1f;    // Atış hızı çarpanı
}

[Serializable]
public class UpgradeData
{
    public string description;   // Kartta yazacak açıklama
    public WeaponType weaponType;
    public UpgradeKind kind;
    public UpgradeRarity rarity; // <-- EKLENDİ
    public int intAmount;
    public float floatAmount;
}

public class WeaponChoiceManager : MonoBehaviour
{
    public static WeaponChoiceManager Instance;   // Singleton

    [Header("Silah Opsiyonları (4 tane doldur)")]
    public WeaponOption[] weapons;   // BUNUN icon alanını kullanıyoruz

    [Header("UI Referansları")]
    public GameObject choicePanel;   // Kartların olduğu ana panel
    public Button cardButton1;
    public Button cardButton2;
    public TMP_Text card1Title;
    public TMP_Text card2Title;

    [Header("Seçim sırasında durdurulacak scriptler")]
    public MonoBehaviour[] scriptsToDisableWhileChoosing; // EnemySpawner, CMBrain, vs

    [Header("Ayarlar")]
    public int maxWeaponSlots = 2;

    // ---- Internal state ----
    private readonly List<WeaponType> ownedWeapons = new List<WeaponType>();
    private bool initialChoiceDone = false;
    private bool panelOpen = false;

    // Weapon seçim modunda kullanılan indexler
    private int currentWeaponIndex1 = -1;
    private int currentWeaponIndex2 = -1;

    // Upgrade seçim modunda kullanılan veriler
    private UpgradeData currentUpgrade1;
    private UpgradeData currentUpgrade2;

    // Runtime statlar artık Inspector’dan değil, DICTIONARY’den gelir
    private Dictionary<WeaponType, WeaponRuntimeStats> statsDict
        = new Dictionary<WeaponType, WeaponRuntimeStats>();

    // -------------------- CURSOR + PAUSE CACHE (EKLENDİ) --------------------
    private float prevTimeScale = 1f;
    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;
    private bool pausedByWeaponChoice = false;
    // ----------------------------------------------------------------------

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildStatsDictionary();

        // Başta bütün silahları kapat
        DisableAllWeapons();

        // Oyuncu kart seçene kadar bazı sistemler çalışmasın
        SetExtraScriptsEnabled(false);
    }

    private void Start()
    {
        OpenInitialChoice();
    }

    // Panel açıkken cursor'un asla kaybolmaması için zorla (EKLENDİ)
    private void LateUpdate()
    {
        if (!panelOpen) return;

        if (!Cursor.visible) Cursor.visible = true;
        if (Cursor.lockState != CursorLockMode.None) Cursor.lockState = CursorLockMode.None;
    }

    #region STATS DICTIONARY

    private void BuildStatsDictionary()
    {
        statsDict.Clear();

        // Tüm WeaponType enum değerleri için default stat oluştur
        foreach (WeaponType t in Enum.GetValues(typeof(WeaponType)))
        {
            if (!statsDict.ContainsKey(t))
            {
                statsDict.Add(t, new WeaponRuntimeStats
                {
                    type = t,
                    extraCount = 0,
                    damageBonus = 0f,
                    attackSpeedMultiplier = 1f
                });
            }
        }

        Debug.Log("[WeaponChoiceManager] Stats dictionary oluşturuldu.");
    }

    private WeaponRuntimeStats GetRuntimeStats(WeaponType t)
    {
        WeaponRuntimeStats s;
        if (statsDict.TryGetValue(t, out s))
            return s;
        return null;
    }

    // PUBLIC yapıldı ki HUD erişebilsin
    public WeaponOption GetWeaponOption(WeaponType t)
    {
        if (weapons == null) return null;

        foreach (var w in weapons)
        {
            if (w != null && w.type == t)
                return w;
        }

        return null;
    }

    #endregion

    #region INITIAL CHOICE (Oyun başı ilk silah seçimi)

    private void OpenInitialChoice()
    {
        if (weapons == null || weapons.Length < 2)
        {
            Debug.LogError("[WeaponChoiceManager] En az 2 silah tanımlaman gerekiyor!");
            return;
        }

        panelOpen = true;

        // --- EKLENDİ: OYUNU DURDUR + CURSOR AÇ ---
        PauseGameAndShowCursor();
        // ----------------------------------------

        SetExtraScriptsEnabled(false);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        // 4 silahtan rastgele 2 tanesini seç
        PickTwoRandomWeapons(out currentWeaponIndex1, out currentWeaponIndex2);
        SetupWeaponChoiceCards(currentWeaponIndex1, currentWeaponIndex2);
    }

    private void PickTwoRandomWeapons(out int index1, out int index2)
    {
        int count = weapons.Length;
        index1 = UnityEngine.Random.Range(0, count);

        do
        {
            index2 = UnityEngine.Random.Range(0, count);
        } while (index2 == index1);
    }

    private void SetupWeaponChoiceCards(int idx1, int idx2)
    {
        // Buton listenerları
        if (cardButton1 != null)
        {
            cardButton1.onClick.RemoveAllListeners();
            cardButton1.onClick.AddListener(() => OnWeaponSelected(idx1));
        }

        if (cardButton2 != null)
        {
            cardButton2.onClick.RemoveAllListeners();
            cardButton2.onClick.AddListener(() => OnWeaponSelected(idx2));
        }

        SetupWeaponCardUI(idx1, cardButton1, card1Title);
        SetupWeaponCardUI(idx2, cardButton2, card2Title);
    }

    private void SetupWeaponCardUI(int idx, Button button, TMP_Text title)
    {
        if (weapons == null || idx < 0 || idx >= weapons.Length) return;

        var w = weapons[idx];
        if (title != null) title.text = w.weaponName;

        // ICON → Butonun kendi Image'ından, WeaponOption.icon kullan
        if (button != null)
        {
            Image img = button.GetComponent<Image>();
            if (img != null && w.icon != null)
            {
                img.sprite = w.icon;
            }
        }
    }

    private void OnWeaponSelected(int weaponIndex)
    {
        if (weapons == null || weaponIndex < 0 || weaponIndex >= weapons.Length)
            return;

        var w = weapons[weaponIndex];

        // Bu silahı aktif et
        EnableWeapon(weaponIndex);
        AddOwnedWeapon(w.type);

        if (!initialChoiceDone)
        {
            initialChoiceDone = true;
        }

        ClosePanel();
    }

    #endregion

    #region LEVEL UP → YENİ SEÇİM

    public void OnPlayerLevelUp(int newLevel)
    {
        if (!initialChoiceDone) return; // İlk seçim yapılmadan level up geldi ise es geç
        OpenLevelUpChoice();
    }

    private void OpenLevelUpChoice()
    {
        if (ownedWeapons.Count == 0)
        {
            OpenInitialChoice();
            return;
        }

        panelOpen = true;

        // --- EKLENDİ: OYUNU DURDUR + CURSOR AÇ ---
        PauseGameAndShowCursor();
        // ----------------------------------------

        SetExtraScriptsEnabled(false);

        if (choicePanel != null)
            choicePanel.SetActive(true);

        // Eğer 2 slot dolmamışsa → yeni silah öner
        if (ownedWeapons.Count < maxWeaponSlots)
        {
            SetupNewWeaponChoices();
        }
        else
        {
            // 2 silahı da seçmiş → upgrade kartları göster
            SetupUpgradeChoices();
        }
    }

    private void SetupNewWeaponChoices()
    {
        // Sahip olunmayan silahları listele
        List<int> candidates = new List<int>();
        for (int i = 0; i < weapons.Length; i++)
        {
            if (!ownedWeapons.Contains(weapons[i].type))
            {
                candidates.Add(i);
            }
        }

        if (candidates.Count == 0)
        {
            SetupUpgradeChoices();
            return;
        }

        int idx1, idx2;

        if (candidates.Count == 1)
        {
            idx1 = candidates[0];
            idx2 = candidates[0];
        }
        else
        {
            idx1 = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            int temp;
            do
            {
                temp = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            } while (temp == idx1);
            idx2 = temp;
        }

        currentWeaponIndex1 = idx1;
        currentWeaponIndex2 = idx2;

        if (cardButton1 != null)
        {
            cardButton1.onClick.RemoveAllListeners();
            cardButton1.onClick.AddListener(() => OnWeaponSelected(idx1));
        }

        if (cardButton2 != null)
        {
            cardButton2.onClick.RemoveAllListeners();
            cardButton2.onClick.AddListener(() => OnWeaponSelected(idx2));
        }

        SetupWeaponCardUI(idx1, cardButton1, card1Title);
        SetupWeaponCardUI(idx2, cardButton2, card2Title);
    }

    private void SetupUpgradeChoices()
    {
        currentUpgrade1 = GenerateRandomUpgrade();
        currentUpgrade2 = GenerateRandomUpgrade();

        if (cardButton1 != null)
        {
            cardButton1.onClick.RemoveAllListeners();
            cardButton1.onClick.AddListener(() => OnUpgradeSelected(1));
        }

        if (cardButton2 != null)
        {
            cardButton2.onClick.RemoveAllListeners();
            cardButton2.onClick.AddListener(() => OnUpgradeSelected(2));
        }

        if (card1Title != null) card1Title.text = currentUpgrade1.description;
        if (card2Title != null) card2Title.text = currentUpgrade2.description;

        SetupUpgradeCardIcon(currentUpgrade1, cardButton1);
        SetupUpgradeCardIcon(currentUpgrade2, cardButton2);
    }

    private void SetupUpgradeCardIcon(UpgradeData data, Button button)
    {
        if (button == null || data == null) return;

        var opt = GetWeaponOption(data.weaponType);
        if (opt != null && opt.icon != null)
        {
            Image img = button.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = opt.icon;
            }
        }
    }

    private void OnUpgradeSelected(int cardIndex)
    {
        UpgradeData chosen = null;
        if (cardIndex == 1) chosen = currentUpgrade1;
        else if (cardIndex == 2) chosen = currentUpgrade2;

        if (chosen != null)
        {
            ApplyUpgrade(chosen);
        }

        ClosePanel();
    }

    // luck01 = 0..1, Legendary max %90 (asla %100 garanti değil)
    private UpgradeRarity RollRarityByLuck(float luck01)
    {
        luck01 = Mathf.Clamp01(luck01);

        // luck düşükken
        float[] low = { 0.55f, 0.25f, 0.13f, 0.05f, 0.02f };

        // luck yüksekken -> Legendary %90'a kadar
        float[] high = { 0.03f, 0.03f, 0.02f, 0.02f, 0.90f };

        float r = UnityEngine.Random.value;
        float acc = 0f;

        for (int i = 0; i < 5; i++)
        {
            float w = Mathf.Lerp(low[i], high[i], luck01);
            acc += w;

            if (r <= acc)
                return (UpgradeRarity)i;
        }

        return UpgradeRarity.Legendary;
    }

    private string RarityName(UpgradeRarity r)
    {
        return r.ToString();
    }

    private UpgradeData GenerateRandomUpgrade()
    {
        if (ownedWeapons.Count == 0)
            ownedWeapons.Add(WeaponType.Bone);

        WeaponType wType = ownedWeapons[UnityEngine.Random.Range(0, ownedWeapons.Count)];
        UpgradeKind kind = (UpgradeKind)UnityEngine.Random.Range(0, 3);

        float luck01 = 0f;
        if (PlayerLuck.Instance != null)
        {
            // Artık Luck01 PlayerLuck içinde var (CS1061 fix)
            luck01 = PlayerLuck.Instance.Luck01;
        }

        UpgradeRarity rarity = RollRarityByLuck(luck01);

        UpgradeData data = new UpgradeData();
        data.weaponType = wType;
        data.kind = kind;
        data.rarity = rarity;

        int rarityIndex = (int)rarity; // 0..4
        string rName = RarityName(rarity);

        int[] countTable = { 1, 2, 3, 4, 5 };
        float[] atkSpdTable = { 0.05f, 0.10f, 0.20f, 0.30f, 0.50f };
        int[] dmgTable = { 5, 10, 15, 20, 50 };

        switch (kind)
        {
            case UpgradeKind.Count:
                {
                    int amount = countTable[rarityIndex];
                    data.intAmount = amount;
                    data.description = $"{rName}: {wType} mermi sayısı +{amount}";
                    break;
                }

            case UpgradeKind.AttackSpeed:
                {
                    float perc = atkSpdTable[rarityIndex];
                    data.floatAmount = perc;
                    data.description = $"{rName}: {wType} saldırı hızı %{Mathf.RoundToInt(perc * 100)} artar";
                    break;
                }

            case UpgradeKind.Damage:
                {
                    int dmg = dmgTable[rarityIndex];
                    data.intAmount = dmg;
                    data.description = $"{rName}: {wType} hasarı +{dmg}";
                    break;
                }
        }

        return data;
    }

    #endregion

    #region UPGRADE UYGULAMA + RUNTIME STATS

    private void ApplyUpgrade(UpgradeData data)
    {
        Debug.Log($"[WeaponChoiceManager] Upgrade seçildi: {data.description}");

        var stats = GetRuntimeStats(data.weaponType);
        if (stats == null)
        {
            Debug.LogWarning($"[WeaponChoiceManager] {data.weaponType} için runtime stat bulunamadı!");
            return;
        }

        switch (data.kind)
        {
            case UpgradeKind.Count:
                stats.extraCount += data.intAmount;
                Debug.Log($"[WeaponChoiceManager] {data.weaponType} extraCount +{data.intAmount} => {stats.extraCount}");
                break;

            case UpgradeKind.AttackSpeed:
                stats.attackSpeedMultiplier *= (1f + data.floatAmount);
                Debug.Log($"[WeaponChoiceManager] {data.weaponType} attackSpeedMultiplier x{1f + data.floatAmount} => {stats.attackSpeedMultiplier}");
                break;

            case UpgradeKind.Damage:
                stats.damageBonus += data.intAmount;
                Debug.Log($"[WeaponChoiceManager] {data.weaponType} damageBonus +{data.intAmount} => {stats.damageBonus}");
                break;
        }
    }

    public float GetModifiedDamage(WeaponType type, float baseDamage)
    {
        var stats = GetRuntimeStats(type);
        if (stats == null)
        {
            Debug.LogWarning($"[WeaponChoiceManager] GetModifiedDamage: {type} için stats bulunamadı, baseDamage dönüyorum.");
            return baseDamage;
        }

        // ---- GLOBAL DAMAGE EKLENDİ (senin istediğin snippet) ----
        float global = PlayerPermanentUpgrades.Instance != null
            ? PlayerPermanentUpgrades.Instance.globalDamageBonus
            : 0f;

        float result = baseDamage + stats.damageBonus + global;
        // --------------------------------------------------------

        // Sandık "Kılıç" çarpanı varsa uygula (yoksa 1)
        float mul = PlayerPermanentUpgrades.Instance != null
            ? PlayerPermanentUpgrades.Instance.globalDamageMultiplier
            : 1f;

        result *= (mul <= 0f ? 1f : mul);

        Debug.Log($"[WeaponChoiceManager] GetModifiedDamage({type}): base={baseDamage}, bonus={stats.damageBonus}, global={global}, mul={mul}, result={result}");
        return result;
    }

    public float GetAttackSpeedMultiplier(WeaponType type)
    {
        var stats = GetRuntimeStats(type);
        if (stats == null) return 1f;
        return stats.attackSpeedMultiplier <= 0f ? 1f : stats.attackSpeedMultiplier;
    }

    public int GetExtraCount(WeaponType type)
    {
        var stats = GetRuntimeStats(type);
        if (stats == null) return 0;
        return stats.extraCount;
    }

    #endregion

    #region WEAPON ENABLE/DISABLE + DİĞER SCRIPTLER

    private void DisableAllWeapons()
    {
        if (weapons == null) return;

        foreach (var w in weapons)
        {
            if (w != null && w.shooterScript != null)
            {
                w.shooterScript.enabled = false;
            }
        }
    }

    private void EnableWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;

        var w = weapons[index];
        if (w != null && w.shooterScript != null)
        {
            w.shooterScript.enabled = true;
        }
    }

    private void AddOwnedWeapon(WeaponType type)
    {
        if (!ownedWeapons.Contains(type))
        {
            ownedWeapons.Add(type);

            if (WeaponHUDIcons.Instance != null)
            {
                WeaponHUDIcons.Instance.OnWeaponAcquired(type);
            }
        }
    }

    private void SetExtraScriptsEnabled(bool enabled)
    {
        if (scriptsToDisableWhileChoosing == null) return;

        foreach (var s in scriptsToDisableWhileChoosing)
        {
            if (s != null)
                s.enabled = enabled;
        }
    }

    private void ClosePanel()
    {
        panelOpen = false;

        if (choicePanel != null)
            choicePanel.SetActive(false);

        // --- EKLENDİ: OYUNU DEVAM ETTİR + CURSOR ESKİ HALİNE ---
        ResumeGameAndRestoreCursor();
        // ------------------------------------------------------

        SetExtraScriptsEnabled(true);
    }

    #endregion

    // -------------------- CURSOR/PAUSE HELPERS (EKLENDİ) --------------------
    private void PauseGameAndShowCursor()
    {
        if (pausedByWeaponChoice) return;
        pausedByWeaponChoice = true;

        prevTimeScale = Time.timeScale;
        prevLockMode = Cursor.lockState;
        prevCursorVisible = Cursor.visible;

        Time.timeScale = 0f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void ResumeGameAndRestoreCursor()
    {
        if (!pausedByWeaponChoice) return;
        pausedByWeaponChoice = false;

        Time.timeScale = prevTimeScale;
        Cursor.visible = prevCursorVisible;
        Cursor.lockState = prevLockMode;
    }

    private void OnDisable()
    {
        // UI kapanırsa oyun kilitli kalmasın
        if (pausedByWeaponChoice)
        {
            ResumeGameAndRestoreCursor();
        }
    }
    // ----------------------------------------------------------------------
}
