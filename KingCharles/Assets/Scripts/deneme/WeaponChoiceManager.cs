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

[Serializable]
public class WeaponOption
{
    public string weaponName;             // Kartta görünen isim
    public WeaponType type;              // Hangi silah
    public MonoBehaviour shooterScript;  // Player üzerindeki AutoShooter
    public Sprite icon;                  // Kart görseli
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
    public int intAmount;
    public float floatAmount;
}

public class WeaponChoiceManager : MonoBehaviour
{
    public static WeaponChoiceManager Instance;   // Singleton

    [Header("Silah Opsiyonları (4 tane doldur)")]
    public WeaponOption[] weapons;

    [Header("Runtime Statlar")]
    public WeaponRuntimeStats[] runtimeStats;

    [Header("UI Referansları")]
    public GameObject choicePanel;   // Kartların olduğu ana panel
    public Button cardButton1;
    public Button cardButton2;
    public TMP_Text card1Title;
    public TMP_Text card2Title;
    // ICON alanı yok; butonun kendi Image’ını kullanıyoruz.

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

    private void Awake()
    {
        Instance = this;

        // Başta bütün silahları kapat
        DisableAllWeapons();

        // Oyuncu kart seçene kadar bazı sistemler çalışmasın
        SetExtraScriptsEnabled(false);
    }

    private void Start()
    {
        OpenInitialChoice();
    }

    #region INITIAL CHOICE (Oyun başı ilk silah seçimi)

    private void OpenInitialChoice()
    {
        if (weapons == null || weapons.Length < 2)
        {
            Debug.LogError("[WeaponChoiceManager] En az 2 silah tanımlaman gerekiyor!");
            return;
        }

        panelOpen = true;
        Time.timeScale = 0f;
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

        SetupWeaponCardUI(idx1, card1Title, cardButton1);
        SetupWeaponCardUI(idx2, card2Title, cardButton2);
    }

    private void SetupWeaponCardUI(int idx, TMP_Text title, Button button)
    {
        if (weapons == null || idx < 0 || idx >= weapons.Length) return;

        var w = weapons[idx];

        if (title != null)
            title.text = w.weaponName;

        // Kart görseli: butonun kendi Image'ı
        if (button != null && button.image != null && w.icon != null)
        {
            button.image.sprite = w.icon;
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

    /// <summary>
    /// PlayerXP.AddXP içinde, level arttığında burayı çağır:
    /// WeaponChoiceManager.Instance?.OnPlayerLevelUp(level);
    /// </summary>
    public void OnPlayerLevelUp(int newLevel)
    {
        if (!initialChoiceDone) return; // İlk seçim yapılmadan level up geldi ise es geç

        OpenLevelUpChoice();
    }

    private void OpenLevelUpChoice()
    {
        if (ownedWeapons.Count == 0)
        {
            // Güvenlik için; normalde olmaz
            OpenInitialChoice();
            return;
        }

        panelOpen = true;
        Time.timeScale = 0f;
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
            // Her şeyi almış → upgrade moduna geç
            SetupUpgradeChoices();
            return;
        }

        int idx1, idx2;

        if (candidates.Count == 1)
        {
            idx1 = candidates[0];
            idx2 = candidates[0]; // İkisini de aynı göstermek istersen; gerekirse değiştirirsin
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

        // Butonlar yine OnWeaponSelected kullanacak
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

        SetupWeaponCardUI(idx1, card1Title, cardButton1);
        SetupWeaponCardUI(idx2, card2Title, cardButton2);
    }

    private void SetupUpgradeChoices()
    {
        // Owned silahlar üzerinden 2 upgrade üret
        currentUpgrade1 = GenerateRandomUpgrade();
        currentUpgrade2 = GenerateRandomUpgrade();

        // Butonlar artık OnUpgradeSelected kullanacak
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

        // UI: açıklama
        if (card1Title != null) card1Title.text = currentUpgrade1.description;
        if (card2Title != null) card2Title.text = currentUpgrade2.description;

        // Kart görselleri: ilgili silahın icon'u → buton sprite
        var w1 = GetWeaponOption(currentUpgrade1.weaponType);
        var w2 = GetWeaponOption(currentUpgrade2.weaponType);

        if (cardButton1 != null && cardButton1.image != null && w1 != null && w1.icon != null)
            cardButton1.image.sprite = w1.icon;

        if (cardButton2 != null && cardButton2.image != null && w2 != null && w2.icon != null)
            cardButton2.image.sprite = w2.icon;
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

    private UpgradeData GenerateRandomUpgrade()
    {
        if (ownedWeapons.Count == 0)
            ownedWeapons.Add(WeaponType.Bone); // Güvenlik

        // Hangi silaha gelecek?
        WeaponType wType = ownedWeapons[UnityEngine.Random.Range(0, ownedWeapons.Count)];

        // Ne tür upgrade?
        UpgradeKind kind = (UpgradeKind)UnityEngine.Random.Range(0, 3);

        UpgradeData data = new UpgradeData();
        data.weaponType = wType;
        data.kind = kind;

        // Basit rarity / değer mantığı
        switch (kind)
        {
            case UpgradeKind.Count:
                {
                    int amount = UnityEngine.Random.value < 0.5f ? 1 : 2;
                    string rarity = amount == 1 ? "Common" : "Rare";
                    data.intAmount = amount;
                    data.description = $"{rarity}: {wType} mermi sayısı +{amount}";
                    break;
                }
            case UpgradeKind.AttackSpeed:
                {
                    float perc = UnityEngine.Random.value < 0.5f ? 0.10f : 0.20f; // %10 veya %20
                    string rarity = perc < 0.15f ? "Common" : "Rare";
                    data.floatAmount = perc;
                    data.description = $"{rarity}: {wType} saldırı hızı %{Mathf.RoundToInt(perc * 100)} artar";
                    break;
                }
            case UpgradeKind.Damage:
                {
                    int dmg = UnityEngine.Random.value < 0.5f ? 5 : 10;
                    string rarity = dmg == 5 ? "Common" : "Rare";
                    data.intAmount = dmg;
                    data.description = $"{rarity}: {wType} hasarı +{dmg}";
                    break;
                }
        }

        return data;
    }

    #endregion

    #region UPGRADE UYGULAMA + RUNTIME STATS

    private WeaponRuntimeStats GetRuntimeStats(WeaponType t)
    {
        if (runtimeStats == null) return null;

        foreach (var s in runtimeStats)
        {
            if (s != null && s.type == t)
                return s;
        }
        return null;
    }

    private WeaponOption GetWeaponOption(WeaponType t)
    {
        if (weapons == null) return null;

        foreach (var w in weapons)
        {
            if (w != null && w.type == t)
                return w;
        }

        return null;
    }

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
                Debug.Log($"[WeaponChoiceManager] {data.weaponType} extraCount +{data.intAmount} → {stats.extraCount}");
                break;

            case UpgradeKind.AttackSpeed:
                stats.attackSpeedMultiplier *= (1f + data.floatAmount);
                Debug.Log($"[WeaponChoiceManager] {data.weaponType} attackSpeedMultiplier x{1f + data.floatAmount} → {stats.attackSpeedMultiplier}");
                break;

            case UpgradeKind.Damage:
                stats.damageBonus += data.intAmount;
                Debug.Log($"[WeaponChoiceManager] {data.weaponType} damageBonus +{data.intAmount} → {stats.damageBonus}");
                break;
        }
    }

    /// <summary>
    /// Verilen silah türü için, base damage üzerine upgrade bonuslarını uygular.
    /// Projectile'lar bunu kullanıyor.
    /// </summary>
    public float GetModifiedDamage(WeaponType type, float baseDamage)
    {
        var stats = GetRuntimeStats(type);
        if (stats == null) return baseDamage;

        float result = baseDamage + stats.damageBonus;
        return result;
    }

    /// <summary>
    /// AutoShooter scriptleri için: atış hızı çarpanı
    /// </summary>
    public float GetAttackSpeedMultiplier(WeaponType type)
    {
        var stats = GetRuntimeStats(type);
        if (stats == null) return 1f;
        return stats.attackSpeedMultiplier <= 0f ? 1f : stats.attackSpeedMultiplier;
    }

    /// <summary>
    /// Fazladan mermi sayısı (multi-shot için)
    /// </summary>
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
            ownedWeapons.Add(type);
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

        Time.timeScale = 1f;
        SetExtraScriptsEnabled(true);
    }

    #endregion
}
