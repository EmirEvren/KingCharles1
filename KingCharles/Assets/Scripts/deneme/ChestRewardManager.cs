using System;
using System.Collections.Generic;
using UnityEngine;

public enum ChestRarity { Common, Uncommon, Rare, Epic, Legendary }
public enum ChestItemType
{
    Horseshoe,      // At nalý -> luck
    GoldIngot,      // altýn külçe -> gold gain
    Hourglass,      // kum saati -> xp gain
    Sword,          // kýlýç -> global damage multiplier
    BullSkull,      // boða kafa tasý -> difficulty multiplier
    StickyBone,     // legendary-only -> ricochet 3
    GreyhoundTooth, // legendary-only -> 5% one-shot (non-boss)
    BloodScent      // legendary-only -> execute 20%
}

[Serializable]
public class ChestItemIcon
{
    public ChestItemType type;
    public string displayName;
    public Sprite icon;
}

public struct ChestReward
{
    public ChestItemType type;
    public ChestRarity rarity;
    public int value;            // tier value (ör. 25/50/.. veya 5/10/..), legendary-only ise 0 olabilir
    public string displayName;
    public Sprite icon;
}

public class ChestRewardManager : MonoBehaviour
{
    public static ChestRewardManager Instance;

    [Header("Icons & Names (Assign in Inspector)")]
    public List<ChestItemIcon> items = new List<ChestItemIcon>();

    // Tier deðerleri
    private readonly int[] horseshoeVals = { 25, 50, 100, 250, 500 }; // Legendary = 500
    private readonly int[] standardVals = { 5, 10, 15, 25, 50 }; // Legendary = 50

    private void Awake()
    {
        Instance = this;
    }

    public ChestReward RollReward()
    {
        int luck = (PlayerLuck.Instance != null) ? PlayerLuck.Instance.luckLevel : 0;

        ChestRarity rarity = RollRarity(luck);
        ChestItemType type = RollItemType(rarity);

        var meta = GetMeta(type);

        ChestReward reward = new ChestReward
        {
            type = type,
            rarity = rarity,
            displayName = meta.displayName,
            icon = meta.icon,
            value = 0
        };

        // Tier value belirle
        if (type == ChestItemType.Horseshoe)
        {
            reward.value = GetTierValue(horseshoeVals, rarity);
        }
        else if (type == ChestItemType.GoldIngot ||
                 type == ChestItemType.Hourglass ||
                 type == ChestItemType.Sword ||
                 type == ChestItemType.BullSkull)
        {
            reward.value = GetTierValue(standardVals, rarity);
        }
        else
        {
            // Legendary-only özel item
            reward.value = 0;
        }

        return reward;
    }

    public void ApplyReward(ChestReward reward)
    {
        // Luck
        if (reward.type == ChestItemType.Horseshoe)
        {
            if (PlayerLuck.Instance != null)
                PlayerLuck.Instance.AddLuck(reward.value);
            return;
        }

        if (PlayerPermanentUpgrades.Instance == null) return;
        var up = PlayerPermanentUpgrades.Instance;

        switch (reward.type)
        {
            case ChestItemType.GoldIngot:
                up.goldGainBonus += reward.value;
                break;

            case ChestItemType.Hourglass:
                up.xpGainBonus += reward.value;
                break;

            case ChestItemType.Sword:
                // “genel hasar çarpaný” => %5/%10/... gibi düþünerek uygula
                up.AddGlobalDamageMultiplierPercent(reward.value);
                break;

            case ChestItemType.BullSkull:
                up.AddDifficultyPercent(reward.value);
                break;

            case ChestItemType.StickyBone:
                up.ricochetBounces = 3; // tek tür legendary
                break;

            case ChestItemType.GreyhoundTooth:
                up.hasGreyhoundTooth = true; // %5 one-shot (non-boss)
                break;

            case ChestItemType.BloodScent:
                up.hasBloodScent = true;     // %20 altý execute
                break;
        }
    }

    // ----------------- INTERNAL -----------------

    private (string displayName, Sprite icon) GetMeta(ChestItemType type)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].type == type)
                return (items[i].displayName, items[i].icon);
        }
        return (type.ToString(), null);
    }

    private int GetTierValue(int[] arr, ChestRarity rarity)
    {
        int idx = rarity switch
        {
            ChestRarity.Common => 0,
            ChestRarity.Uncommon => 1,
            ChestRarity.Rare => 2,
            ChestRarity.Epic => 3,
            ChestRarity.Legendary => 4,
            _ => 0
        };
        idx = Mathf.Clamp(idx, 0, arr.Length - 1);
        return arr[idx];
    }

    private ChestRarity RollRarity(int luck)
    {
        // Legendary chance: 2% taban, luck ile artar, 90% cap
        float pLegendary = 0.02f + 0.88f * (1f - Mathf.Exp(-luck / 25f));
        pLegendary = Mathf.Clamp(pLegendary, 0.02f, 0.90f);

        float r = UnityEngine.Random.value;
        if (r < pLegendary) return ChestRarity.Legendary;

        float remaining = 1f - pLegendary;

        // Kalaný sabit oranlarla daðýtýyoruz
        float pCommon = remaining * 0.45f;
        float pUncommon = remaining * 0.25f;
        float pRare = remaining * 0.18f;
        float pEpic = remaining * 0.12f;

        float x = r - pLegendary;
        if (x < pCommon) return ChestRarity.Common;
        x -= pCommon;
        if (x < pUncommon) return ChestRarity.Uncommon;
        x -= pUncommon;
        if (x < pRare) return ChestRarity.Rare;
        return ChestRarity.Epic;
    }

    private ChestItemType RollItemType(ChestRarity rarity)
    {
        // Normal havuz (legendary deðilse)
        ChestItemType[] normalPool =
        {
            ChestItemType.Horseshoe,
            ChestItemType.GoldIngot,
            ChestItemType.Hourglass,
            ChestItemType.Sword,
            ChestItemType.BullSkull
        };

        // Legendary gelirse: normal pool + 3 özel legendary
        ChestItemType[] legendaryPool =
        {
            ChestItemType.Horseshoe,
            ChestItemType.GoldIngot,
            ChestItemType.Hourglass,
            ChestItemType.Sword,
            ChestItemType.BullSkull,
            ChestItemType.StickyBone,
            ChestItemType.GreyhoundTooth,
            ChestItemType.BloodScent
        };

        var pool = (rarity == ChestRarity.Legendary) ? legendaryPool : normalPool;
        return pool[UnityEngine.Random.Range(0, pool.Length)];
    }
}
