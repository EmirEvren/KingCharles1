using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    public int tierIndex;         // 0..4
    public string rarityName;     // Common..Legendary
    public int intValue;
    public float floatValue;

    public string GetTitle()
    {
        return type switch
        {
            DogUpgradeType.XPGainRate => "XP Gain Rate",
            DogUpgradeType.GoldGainRate => "Gold Gain Rate",
            DogUpgradeType.GlobalWeaponDamage => "All Weapons Damage",
            DogUpgradeType.MoveSpeed => "Move Speed",
            DogUpgradeType.Luck => "Luck",
            DogUpgradeType.MaxHealth => "Max Health",
            _ => "Upgrade"
        };
    }

    public string GetDesc()
    {
        return type switch
        {
            DogUpgradeType.XPGainRate => "(Yerden toplanan XP kürelerinden alýnan XP miktarýný arttýrýr)",
            DogUpgradeType.GoldGainRate => "(Yerden toplanan coinlerden alýnan altýn miktarýný arttýrýr)",
            DogUpgradeType.GlobalWeaponDamage => "(Tüm silahlarýn hasarýný arttýrýr)",
            DogUpgradeType.MoveSpeed => "(Oyuncu hareket hýzýný arttýrýr)",
            DogUpgradeType.Luck => "(Þans deðerini arttýrýr)",
            DogUpgradeType.MaxHealth => "(Oyuncu maksimum can deðerini arttýrýr)",
            _ => ""
        };
    }

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

    public string GetButtonText()
    {
        return $"{rarityName}: {GetTitle()} {GetValueText()}\n{GetDesc()}";
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
    public Color uncommonColor = new Color(0.30f, 0.85f, 0.35f, 1f);   // yeþil
    public Color rareColor = new Color(0.30f, 0.55f, 0.95f, 1f);       // mavi
    public Color epicColor = new Color(0.65f, 0.30f, 0.95f, 1f);       // mor
    public Color legendaryColor = new Color(0.95f, 0.80f, 0.15f, 1f);  // sarý

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
        if (txt != null) txt.text = opt.GetButtonText();

        // --- EKLENDÝ: rarity'e göre buton rengi ---
        ApplyRarityColor(btn, opt.tierIndex);
        // -----------------------------------------

        if (btn == null) return;

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => OnPick(index));
    }

    private void ApplyRarityColor(Button btn, int tierIndex)
    {
        if (btn == null) return;

        Image btnImg = btn.GetComponent<Image>();
        if (btnImg == null) return;

        Color c = commonColor;

        // tierIndex: 0=Common, 1=Uncommon, 2=Rare, 3=Epic, 4=Legendary
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

        // Diðerleri
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
