using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;            // <-- EKLENDİ
using UnityEngine.Localization.Settings;   // <-- EKLENDİ

public enum DogUpgradeType
{
    XPGainRate,
    GoldGainRate,
    GlobalWeaponDamage,
    MoveSpeed,
    Luck,
    MaxHealth
}

[Serializable]
public struct DogHouseUpgradeOption
{
    public DogUpgradeType type;
    public int tierIndex;         // 0..4 (0=Common, 4=Legendary)
    public string rarityName;     // (Artık bunu kullanmayacağız, koddan çevireceğiz)
    public int intValue;
    public float floatValue;

    // Value text'i sayı olduğu için buradan almaya devam edebiliriz
    public string GetValueText()
    {
        return type switch
        {
            DogUpgradeType.MoveSpeed => $"+{floatValue:0.##}",
            DogUpgradeType.GlobalWeaponDamage => $"+{intValue}",
            DogUpgradeType.XPGainRate => $"+{intValue}",
            DogUpgradeType.GoldGainRate => $"+{intValue}",
            DogUpgradeType.Luck => $"+{intValue}",
            DogUpgradeType.MaxHealth => $"+{intValue}",
            _ => ""
        };
    }
}

public class DogHouseUpgradeUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject panel;

    public Button button1;
    public Button button2;
    public Button button3;

    public TMP_Text text1;
    public TMP_Text text2;
    public TMP_Text text3;

    [Header("Rarity Colors (Buttons)")]
    public Color commonColor = new Color(0.75f, 0.75f, 0.75f, 1f);     // gri
    public Color uncommonColor = new Color(0.30f, 0.85f, 0.35f, 1f);   // yeşil
    public Color rareColor = new Color(0.30f, 0.55f, 0.95f, 1f);       // mavi
    public Color epicColor = new Color(0.65f, 0.30f, 0.95f, 1f);       // mor
    public Color legendaryColor = new Color(0.95f, 0.80f, 0.15f, 1f);  // sarı

    [Header("Localization Keys (Inspector'dan Seç)")]
    // Başlıklar
    public LocalizedString titleXP;
    public LocalizedString titleGold;
    public LocalizedString titleDamage;
    public LocalizedString titleSpeed;
    public LocalizedString titleLuck;
    public LocalizedString titleHealth;

    // Açıklamalar
    public LocalizedString descXP;
    public LocalizedString descGold;
    public LocalizedString descDamage;
    public LocalizedString descSpeed;
    public LocalizedString descLuck;
    public LocalizedString descHealth;

    // Nadirlik İsimleri
    public LocalizedString rarityCommon;
    public LocalizedString rarityUncommon;
    public LocalizedString rarityRare;
    public LocalizedString rarityEpic;
    public LocalizedString rarityLegendary;


    public Action onClosed;

    private DogHouseUpgradeOption opt1;
    private DogHouseUpgradeOption opt2;
    private DogHouseUpgradeOption opt3;

    public bool IsOpen => panel != null && panel.activeSelf;

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Open(DogHouseUpgradeOption a, DogHouseUpgradeOption b, DogHouseUpgradeOption c)
    {
        opt1 = a; opt2 = b; opt3 = c;

        if (panel != null) panel.SetActive(true);

        Setup(button1, text1, opt1, 1);
        Setup(button2, text2, opt2, 2);
        Setup(button3, text3, opt3, 3);
    }

    private void Setup(Button btn, TMP_Text txt, DogHouseUpgradeOption opt, int index)
    {
        // --- TEXT OLUŞTURMA (Localization ile) ---
        if (txt != null)
        {
            string rName = GetLocalizedRarity(opt.tierIndex); // Common
            string title = GetLocalizedTitle(opt.type);       // XP Gain
            string val = opt.GetValueText();                  // +5
            string desc = GetLocalizedDesc(opt.type);         // (Increases XP...)

            // Format: "Common: XP Gain +5 \n (Increases XP...)"
            txt.text = $"{rName}: {title} {val}\n({desc})";
        }

        // --- RENK AYARLAMA ---
        ApplyRarityColor(btn, opt.tierIndex);

        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnPick(index));
    }

    // --- YARDIMCI ÇEVİRİ FONKSİYONLARI ---

    private string GetLocalizedTitle(DogUpgradeType type)
    {
        switch (type)
        {
            case DogUpgradeType.XPGainRate: return titleXP.GetLocalizedString();
            case DogUpgradeType.GoldGainRate: return titleGold.GetLocalizedString();
            case DogUpgradeType.GlobalWeaponDamage: return titleDamage.GetLocalizedString();
            case DogUpgradeType.MoveSpeed: return titleSpeed.GetLocalizedString();
            case DogUpgradeType.Luck: return titleLuck.GetLocalizedString();
            case DogUpgradeType.MaxHealth: return titleHealth.GetLocalizedString();
            default: return "";
        }
    }

    private string GetLocalizedDesc(DogUpgradeType type)
    {
        switch (type)
        {
            case DogUpgradeType.XPGainRate: return descXP.GetLocalizedString();
            case DogUpgradeType.GoldGainRate: return descGold.GetLocalizedString();
            case DogUpgradeType.GlobalWeaponDamage: return descDamage.GetLocalizedString();
            case DogUpgradeType.MoveSpeed: return descSpeed.GetLocalizedString();
            case DogUpgradeType.Luck: return descLuck.GetLocalizedString();
            case DogUpgradeType.MaxHealth: return descHealth.GetLocalizedString();
            default: return "";
        }
    }

    private string GetLocalizedRarity(int tierIndex)
    {
        switch (tierIndex)
        {
            case 0: return rarityCommon.GetLocalizedString();
            case 1: return rarityUncommon.GetLocalizedString();
            case 2: return rarityRare.GetLocalizedString();
            case 3: return rarityEpic.GetLocalizedString();
            case 4: return rarityLegendary.GetLocalizedString();
            default: return rarityCommon.GetLocalizedString();
        }
    }

    // -------------------------------------

    private void ApplyRarityColor(Button btn, int tierIndex)
    {
        if (btn == null) return;
        Image btnImg = btn.GetComponent<Image>();
        if (btnImg == null) return;

        Color c = commonColor;
        switch (tierIndex)
        {
            case 0: c = commonColor; break;
            case 1: c = uncommonColor; break;
            case 2: c = rareColor; break;
            case 3: c = epicColor; break;
            default: c = legendaryColor; break;
        }
        btnImg.color = c;
    }

    private void OnPick(int index)
    {
        DogHouseUpgradeOption chosen = index switch
        {
            1 => opt1,
            2 => opt2,
            _ => opt3
        };

        Apply(chosen);
        Close();
    }

    private void Apply(DogHouseUpgradeOption opt)
    {
        // Luck
        if (opt.type == DogUpgradeType.Luck)
        {
            if (PlayerLuck.Instance != null)
                PlayerLuck.Instance.AddLuck(opt.intValue);
            return;
        }

        // Diğerleri
        if (PlayerPermanentUpgrades.Instance == null) return;

        switch (opt.type)
        {
            case DogUpgradeType.XPGainRate:
                PlayerPermanentUpgrades.Instance.xpGainBonus += opt.intValue;
                break;

            case DogUpgradeType.GoldGainRate:
                PlayerPermanentUpgrades.Instance.goldGainBonus += opt.intValue;
                break;

            case DogUpgradeType.GlobalWeaponDamage:
                PlayerPermanentUpgrades.Instance.globalDamageBonus += opt.intValue;
                break;

            case DogUpgradeType.MoveSpeed:
                PlayerPermanentUpgrades.Instance.ApplyMoveSpeedBonus(opt.floatValue);
                break;

            case DogUpgradeType.MaxHealth:
                PlayerPermanentUpgrades.Instance.ApplyMaxHealthBonus(opt.intValue);
                break;
        }
    }

    public void Close()
    {
        if (panel != null) panel.SetActive(false);
        onClosed?.Invoke();
    }
}