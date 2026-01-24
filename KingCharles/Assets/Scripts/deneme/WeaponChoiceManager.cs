using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;            // <-- EKLENDİ
using UnityEngine.Localization.Settings;   // <-- EKLENDİ

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
    public string weaponName;             
    public WeaponType type;               
    public MonoBehaviour shooterScript;   
    public Sprite icon;                   
}

[Serializable]
public class WeaponRuntimeStats
{
    public WeaponType type;
    public int extraCount;                  
    public float damageBonus;               
    public float attackSpeedMultiplier = 1f;    
}

[Serializable]
public class UpgradeData
{
    public string description;   // Artık buraya ÇEVRİLMİŞ metin gelecek
    public WeaponType weaponType;
    public UpgradeKind kind;
    public UpgradeRarity rarity; 
    public int intAmount;
    public float floatAmount;
}

public class WeaponChoiceManager : MonoBehaviour
{
    public static WeaponChoiceManager Instance;   

    [Header("Silah Opsiyonları")]
    public WeaponOption[] weapons;   

    [Header("UI Referansları")]
    public GameObject choicePanel;   
    public Button cardButton1;
    public Button cardButton2;
    public TMP_Text card1Title;
    public TMP_Text card2Title;

    // ---------------------------------------------------------------------
    // YENİ EKLENEN LOCALIZATION DEĞİŞKENLERİ (MEVCUT KODU BOZMAZ)
    // ---------------------------------------------------------------------
    [Header("Localization - Yükseltme Kalıpları")]
    public LocalizedString patternCount;  // Key_Pattern_Count
    public LocalizedString patternSpeed;  // Key_Pattern_Speed
    public LocalizedString patternDamage; // Key_Pattern_Damage

    [Header("Localization - Nadirlik İsimleri")]
    public LocalizedString rarityCommon;
    public LocalizedString rarityUncommon;
    public LocalizedString rarityRare;
    public LocalizedString rarityEpic;
    public LocalizedString rarityLegendary;

    [Header("Localization - Silah İsimleri")]
    public LocalizedString nameBone;
    public LocalizedString nameSteak;
    public LocalizedString nameFireball;
    public LocalizedString nameTennisBall;
    // ---------------------------------------------------------------------

    [Header("Diğer Ayarlar")]
    public MonoBehaviour[] scriptsToDisableWhileChoosing; 
    public int maxWeaponSlots = 2;

    // ---- Internal state ----
    private readonly List<WeaponType> ownedWeapons = new List<WeaponType>();
    private bool initialChoiceDone = false;
    private bool panelOpen = false;

    private int currentWeaponIndex1 = -1;
    private int currentWeaponIndex2 = -1;

    private UpgradeData currentUpgrade1;
    private UpgradeData currentUpgrade2;

    private Dictionary<WeaponType, WeaponRuntimeStats> statsDict
        = new Dictionary<WeaponType, WeaponRuntimeStats>();

    // Cursor Cache
    private float prevTimeScale = 1f;
    private CursorLockMode prevLockMode;
    private bool prevCursorVisible;
    private bool pausedByWeaponChoice = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildStatsDictionary();
        DisableAllWeapons();
        SetExtraScriptsEnabled(false);
    }

    private void Start()
    {
        OpenInitialChoice();
    }

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
    }

    private WeaponRuntimeStats GetRuntimeStats(WeaponType t)
    {
        if (statsDict.TryGetValue(t, out var s)) return s;
        return null;
    }

    public WeaponOption GetWeaponOption(WeaponType t)
    {
        if (weapons == null) return null;
        foreach (var w in weapons)
        {
            if (w != null && w.type == t) return w;
        }
        return null;
    }
    #endregion

    #region INITIAL CHOICE
    private void OpenInitialChoice()
    {
        if (weapons == null || weapons.Length < 2)
        {
            Debug.LogError("[WeaponChoiceManager] En az 2 silah lazım!");
            return;
        }

        panelOpen = true;
        PauseGameAndShowCursor();
        SetExtraScriptsEnabled(false);

        if (choicePanel != null) choicePanel.SetActive(true);

        PickTwoRandomWeapons(out currentWeaponIndex1, out currentWeaponIndex2);
        
        // Silah Seçimi Çevirileri (RewardTranslator Varsa)
        if (RewardTranslator.Instance != null)
        {
            WeaponType type1 = weapons[currentWeaponIndex1].type;
            WeaponType type2 = weapons[currentWeaponIndex2].type;
            RewardTranslator.Instance.UpdateCardTexts(type1, type2);
        }

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
        
        // Not: Title text RewardTranslator tarafından yönetiliyor.
        
        if (button != null)
        {
            Image img = button.GetComponent<Image>();
            if (img != null && w.icon != null) img.sprite = w.icon;
        }
    }

    private void OnWeaponSelected(int weaponIndex)
    {
        if (weapons == null || weaponIndex < 0 || weaponIndex >= weapons.Length) return;

        var w = weapons[weaponIndex];
        EnableWeapon(weaponIndex);
        AddOwnedWeapon(w.type);

        if (!initialChoiceDone) initialChoiceDone = true;
        ClosePanel();
    }
    #endregion

    #region LEVEL UP
    public void OnPlayerLevelUp(int newLevel)
    {
        if (!initialChoiceDone) return;
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
        PauseGameAndShowCursor();
        SetExtraScriptsEnabled(false);

        if (choicePanel != null) choicePanel.SetActive(true);

        if (ownedWeapons.Count < maxWeaponSlots)
        {
            SetupNewWeaponChoices();
        }
        else
        {
            SetupUpgradeChoices();
        }
    }

    private void SetupNewWeaponChoices()
    {
        List<int> candidates = new List<int>();
        for (int i = 0; i < weapons.Length; i++)
        {
            if (!ownedWeapons.Contains(weapons[i].type)) candidates.Add(i);
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
            do { temp = candidates[UnityEngine.Random.Range(0, candidates.Count)]; } while (temp == idx1);
            idx2 = temp;
        }

        currentWeaponIndex1 = idx1;
        currentWeaponIndex2 = idx2;

        if (RewardTranslator.Instance != null)
        {
            RewardTranslator.Instance.UpdateCardTexts(weapons[idx1].type, weapons[idx2].type);
        }

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

    // --- BURASI GÜNCELLENDİ: ARTIK ÇEVİRİLİ METİNLERİ BASIYOR ---
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

        // Çevrilmiş metinleri (GenerateRandomUpgrade içinde oluşturduk) basıyoruz
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
            if (img != null) img.sprite = opt.icon;
        }
    }

    private void OnUpgradeSelected(int cardIndex)
    {
        UpgradeData chosen = (cardIndex == 1) ? currentUpgrade1 : currentUpgrade2;
        if (chosen != null) ApplyUpgrade(chosen);
        ClosePanel();
    }

    private UpgradeRarity RollRarityByLuck(float luck01)
    {
        luck01 = Mathf.Clamp01(luck01);
        float[] low = { 0.55f, 0.25f, 0.13f, 0.05f, 0.02f };
        float[] high = { 0.03f, 0.03f, 0.02f, 0.02f, 0.90f };

        float r = UnityEngine.Random.value;
        float acc = 0f;
        for (int i = 0; i < 5; i++)
        {
            float w = Mathf.Lerp(low[i], high[i], luck01);
            acc += w;
            if (r <= acc) return (UpgradeRarity)i;
        }
        return UpgradeRarity.Legendary;
    }

    // --- KRİTİK DEĞİŞİKLİK BURADA (LOCALIZATION) ---
    private UpgradeData GenerateRandomUpgrade()
    {
        if (ownedWeapons.Count == 0) ownedWeapons.Add(WeaponType.Bone);

        WeaponType wType = ownedWeapons[UnityEngine.Random.Range(0, ownedWeapons.Count)];
        UpgradeKind kind = (UpgradeKind)UnityEngine.Random.Range(0, 3);

        float luck01 = (PlayerLuck.Instance != null) ? PlayerLuck.Instance.Luck01 : 0f;
        UpgradeRarity rarity = RollRarityByLuck(luck01);
        int rarityIndex = (int)rarity;

        UpgradeData data = new UpgradeData();
        data.weaponType = wType;
        data.kind = kind;
        data.rarity = rarity;

        // Veri Tabloları
        int[] countTable = { 1, 2, 3, 4, 5 };
        float[] atkSpdTable = { 0.05f, 0.10f, 0.20f, 0.30f, 0.50f };
        int[] dmgTable = { 5, 10, 15, 20, 50 };

        // 1. Çevrilmiş Parçaları Alalım (Helper Fonksiyonlar Aşağıda)
        string translatedRarity = GetLocalizedRarity(rarity);
        string translatedWeapon = GetLocalizedWeaponName(wType);
        string finalDesc = "";

        switch (kind)
        {
            case UpgradeKind.Count:
                {
                    int amount = countTable[rarityIndex];
                    data.intAmount = amount;
                    
                    // Kalıbı al ve doldur: "{0}: {1} Ammo +{2}" -> "Yaygın: Kemik Mermi +1"
                    if (patternCount != null && !patternCount.IsEmpty)
                        finalDesc = patternCount.GetLocalizedString(translatedRarity, translatedWeapon, amount);
                    else
                        finalDesc = $"{rarity}: {wType} Count +{amount}"; // Yedek
                    break;
                }

            case UpgradeKind.AttackSpeed:
                {
                    float perc = atkSpdTable[rarityIndex];
                    data.floatAmount = perc;
                    int percInt = Mathf.RoundToInt(perc * 100);
                    
                    if (patternSpeed != null && !patternSpeed.IsEmpty)
                        finalDesc = patternSpeed.GetLocalizedString(translatedRarity, translatedWeapon, percInt);
                    else
                        finalDesc = $"{rarity}: {wType} Speed +{percInt}%";
                    break;
                }

            case UpgradeKind.Damage:
                {
                    int dmg = dmgTable[rarityIndex];
                    data.intAmount = dmg;
                    
                    if (patternDamage != null && !patternDamage.IsEmpty)
                        finalDesc = patternDamage.GetLocalizedString(translatedRarity, translatedWeapon, dmg);
                    else
                        finalDesc = $"{rarity}: {wType} Damage +{dmg}";
                    break;
                }
        }

        data.description = finalDesc;
        return data;
    }

    // --- YARDIMCI ÇEVİRİ FONKSİYONLARI (KODU KİRLETMEMEK İÇİN AYIRDIK) ---
    private string GetLocalizedRarity(UpgradeRarity r)
    {
        switch (r)
        {
            case UpgradeRarity.Common: return (rarityCommon != null && !rarityCommon.IsEmpty) ? rarityCommon.GetLocalizedString() : r.ToString();
            case UpgradeRarity.Uncommon: return (rarityUncommon != null && !rarityUncommon.IsEmpty) ? rarityUncommon.GetLocalizedString() : r.ToString();
            case UpgradeRarity.Rare: return (rarityRare != null && !rarityRare.IsEmpty) ? rarityRare.GetLocalizedString() : r.ToString();
            case UpgradeRarity.Epic: return (rarityEpic != null && !rarityEpic.IsEmpty) ? rarityEpic.GetLocalizedString() : r.ToString();
            case UpgradeRarity.Legendary: return (rarityLegendary != null && !rarityLegendary.IsEmpty) ? rarityLegendary.GetLocalizedString() : r.ToString();
            default: return r.ToString();
        }
    }

    private string GetLocalizedWeaponName(WeaponType t)
    {
        switch (t)
        {
            case WeaponType.Bone: return (nameBone != null && !nameBone.IsEmpty) ? nameBone.GetLocalizedString() : t.ToString();
            case WeaponType.Steak: return (nameSteak != null && !nameSteak.IsEmpty) ? nameSteak.GetLocalizedString() : t.ToString();
            case WeaponType.Fireball: return (nameFireball != null && !nameFireball.IsEmpty) ? nameFireball.GetLocalizedString() : t.ToString();
            case WeaponType.TennisBall: return (nameTennisBall != null && !nameTennisBall.IsEmpty) ? nameTennisBall.GetLocalizedString() : t.ToString();
            default: return t.ToString();
        }
    }
    // --------------------------------------------------------------------

    #endregion

    #region UPGRADE APPLY + HELPERS
    private void ApplyUpgrade(UpgradeData data)
    {
        Debug.Log($"[WeaponChoiceManager] Upgrade: {data.description}");

        var stats = GetRuntimeStats(data.weaponType);
        if (stats == null) return;

        switch (data.kind)
        {
            case UpgradeKind.Count: stats.extraCount += data.intAmount; break;
            case UpgradeKind.AttackSpeed: stats.attackSpeedMultiplier *= (1f + data.floatAmount); break;
            case UpgradeKind.Damage: stats.damageBonus += data.intAmount; break;
        }
    }

    public float GetModifiedDamage(WeaponType type, float baseDamage)
    {
        var stats = GetRuntimeStats(type);
        if (stats == null) return baseDamage;

        float global = (PlayerPermanentUpgrades.Instance != null) ? PlayerPermanentUpgrades.Instance.globalDamageBonus : 0f;
        float result = baseDamage + stats.damageBonus + global;
        float mul = (PlayerPermanentUpgrades.Instance != null) ? PlayerPermanentUpgrades.Instance.globalDamageMultiplier : 1f;
        result *= (mul <= 0f ? 1f : mul);
        return result;
    }

    public float GetAttackSpeedMultiplier(WeaponType type)
    {
        var stats = GetRuntimeStats(type);
        return (stats == null || stats.attackSpeedMultiplier <= 0f) ? 1f : stats.attackSpeedMultiplier;
    }

    public int GetExtraCount(WeaponType type)
    {
        var stats = GetRuntimeStats(type);
        return (stats == null) ? 0 : stats.extraCount;
    }

    private void DisableAllWeapons()
    {
        if (weapons == null) return;
        foreach (var w in weapons) if (w?.shooterScript != null) w.shooterScript.enabled = false;
    }

    private void EnableWeapon(int index)
    {
        if (weapons == null || index < 0 || index >= weapons.Length) return;
        var w = weapons[index];
        if (w?.shooterScript != null) w.shooterScript.enabled = true;
    }

    private void AddOwnedWeapon(WeaponType type)
    {
        if (!ownedWeapons.Contains(type))
        {
            ownedWeapons.Add(type);
            if (WeaponHUDIcons.Instance != null) WeaponHUDIcons.Instance.OnWeaponAcquired(type);
        }
    }

    private void SetExtraScriptsEnabled(bool enabled)
    {
        if (scriptsToDisableWhileChoosing == null) return;
        foreach (var s in scriptsToDisableWhileChoosing) if (s != null) s.enabled = enabled;
    }

    private void ClosePanel()
    {
        panelOpen = false;
        if (choicePanel != null) choicePanel.SetActive(false);
        ResumeGameAndRestoreCursor();
        SetExtraScriptsEnabled(true);
    }

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
        if (pausedByWeaponChoice) ResumeGameAndRestoreCursor();
    }
    #endregion
}