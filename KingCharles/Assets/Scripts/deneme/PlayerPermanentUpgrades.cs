using System.Reflection;
using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class PlayerPermanentUpgrades : MonoBehaviour
{
    public static PlayerPermanentUpgrades Instance;

    [Header("Player Find (Eğer script Player üzerinde değilse)")]
    public string playerTag = "Animal";

    [Header("Kalıcı Bonuslar (Stacklenir)")]
    public int xpGainBonus = 0;                 // XP orb başına +N
    public int goldGainBonus = 0;               // Coin başına +N
    public float globalDamageBonus = 0f;        // Tüm silahlara düz +damage

    [Header("Global Multipliers (Sandık itemları)")]
    public float globalDamageMultiplier = 1f;   // Kılıç: genel hasar çarpanı (1 = normal)
    public float difficultyMultiplier = 1f;     // Boğa kafa tası: zorluk çarpanı (1 = normal)

    [Header("Move Speed (DogHouse / Sandık)")]
    public float moveSpeedBonus = 0f;           // +0.1, +0.15 vb

    [Header("Legendary Flags (Sandık itemları)")]
    public int ricochetBounces = 0;             // Yapışkan Kemik => 3
    public bool hasGreyhoundTooth = false;      // Tazı Diş
    public bool hasBloodScent = false;          // Kan Kokusu

    [Header("Max Health (Malbers Stats)")]
    public StatID healthStatID;                 // Player'ın Health StatID'sini buraya ver

    private Stats stats;
    private Animator anim;
    private float baseAnimatorSpeed = 1f;

    private UnityEngine.AI.NavMeshAgent agent;
    private float baseAgentSpeed = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        CachePlayerReferences();
    }

    private void CachePlayerReferences()
    {
        // Önce bu objede dene
        stats = GetComponent<Stats>();
        anim = GetComponent<Animator>();
        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();

        // Eğer bu objede yoksa playerTag ile bul
        if (stats == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
            if (pObj != null)
            {
                if (stats == null) stats = pObj.GetComponent<Stats>() ?? pObj.GetComponentInParent<Stats>();
                if (anim == null) anim = pObj.GetComponent<Animator>() ?? pObj.GetComponentInParent<Animator>();
                if (agent == null) agent = pObj.GetComponent<UnityEngine.AI.NavMeshAgent>() ?? pObj.GetComponentInParent<UnityEngine.AI.NavMeshAgent>();
            }
        }

        if (anim != null) baseAnimatorSpeed = anim.speed;
        if (agent != null) baseAgentSpeed = agent.speed;
    }

    public int ModifyXP(int baseXP) => Mathf.Max(0, baseXP + xpGainBonus);
    public int ModifyGold(int baseGold) => Mathf.Max(0, baseGold + goldGainBonus);

    // ---- SANDIK: KILIÇ ----
    public void AddGlobalDamageMultiplierPercent(float percent)
    {
        float mul = 1f + (percent / 100f);
        globalDamageMultiplier *= mul;
    }

    // UYUMLULUK (ChestRewardManager vb. farklı isim çağırırsa hata vermesin)
    public void AddGlobalDamagePercent(float percent)
    {
        AddGlobalDamageMultiplierPercent(percent);
    }

    // ---- SANDIK: BOĞA KAFA TASI ----
    public void AddDifficultyMultiplierPercent(float percent)
    {
        float mul = 1f + (percent / 100f);
        difficultyMultiplier *= mul;
    }

    // ✅ CS1061 FIX: ChestRewardManager.cs AddDifficultyPercent çağırıyor
    public void AddDifficultyPercent(float percent)
    {
        AddDifficultyMultiplierPercent(percent);
    }

    public void ApplyMoveSpeedBonus(float add)
    {
        moveSpeedBonus += add;
        ApplyMoveSpeedToController();
    }

    private void ApplyMoveSpeedToController()
    {
        if (stats == null && anim == null && agent == null)
            CachePlayerReferences();

        float newVal = 1f + moveSpeedBonus;

        // 1) Malbers MAnimal varsa AnimatorSpeed.Value setlemeyi dene (reflection)
        var malbersAnimal = GetComponent("MAnimal");
        if (malbersAnimal == null)
        {
            if (!string.IsNullOrEmpty(playerTag))
            {
                GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
                if (pObj != null)
                    malbersAnimal = pObj.GetComponent("MAnimal");
            }
        }

        if (malbersAnimal != null)
        {
            var t = malbersAnimal.GetType();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            FieldInfo animatorSpeedField = t.GetField("AnimatorSpeed", flags);
            object animatorSpeedObj = null;

            if (animatorSpeedField != null)
                animatorSpeedObj = animatorSpeedField.GetValue(malbersAnimal);
            else
            {
                PropertyInfo animatorSpeedProp = t.GetProperty("AnimatorSpeed", flags);
                if (animatorSpeedProp != null && animatorSpeedProp.CanRead)
                    animatorSpeedObj = animatorSpeedProp.GetValue(malbersAnimal, null);
            }

            if (animatorSpeedObj != null)
            {
                var refType = animatorSpeedObj.GetType();
                var valueProp = refType.GetProperty("Value", flags);
                if (valueProp != null && valueProp.CanWrite)
                {
                    valueProp.SetValue(animatorSpeedObj, newVal, null);
                    return;
                }
            }
        }

        // 2) Animator fallback
        if (anim != null)
        {
            anim.speed = baseAnimatorSpeed + moveSpeedBonus;
            return;
        }

        // 3) NavMeshAgent fallback
        if (agent != null)
        {
            agent.speed = baseAgentSpeed + moveSpeedBonus;
        }
    }

    public void ApplyMaxHealthBonus(int addMaxHp)
    {
        if (stats == null || healthStatID == null)
            CachePlayerReferences();

        if (stats == null || healthStatID == null) return;

        var hp = stats.Stat_Get(healthStatID);
        if (hp == null) return;

        hp.ModifyMAX(addMaxHp);
    }
}
