using UnityEngine;
using MalbersAnimations;
using MalbersAnimations.Scriptables;

public class PlayerPermanentUpgrades : MonoBehaviour
{
    public static PlayerPermanentUpgrades Instance;

    [Header("Kalýcý Bonuslar (Stacklenir)")]
    public int xpGainBonus = 0;          // XP orb baþýna +N
    public int goldGainBonus = 0;        // Coin baþýna +N
    public float globalDamageBonus = 0f; // Tüm silahlara düz +damage
    public float moveSpeedBonus = 0f;    // Hýz bonusu (animator / agent speed üzerinden)

    [Header("Max Health (Malbers Stats)")]
    public StatID healthStatID;          // Player'ýn Health StatID'sini buraya ver
    private Stats stats;

    // Move speed fallback cache
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

        stats = GetComponent<Stats>();
        anim = GetComponent<Animator>();
        if (anim != null) baseAnimatorSpeed = anim.speed;

        agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null) baseAgentSpeed = agent.speed;
    }

    public int ModifyXP(int baseXP) => Mathf.Max(0, baseXP + xpGainBonus);
    public int ModifyGold(int baseGold) => Mathf.Max(0, baseGold + goldGainBonus);

    public void ApplyMoveSpeedBonus(float add)
    {
        moveSpeedBonus += add;
        ApplyMoveSpeedToController();
    }

    private void ApplyMoveSpeedToController()
    {
        // 1) Malbers MAnimal varsa AnimatorSpeed.Value'yu setlemeyi dene (reflection ile güvenli)
        var malbersAnimal = GetComponent("MAnimal");
        if (malbersAnimal != null)
        {
            var t = malbersAnimal.GetType();
            var animatorSpeedField = t.GetField("AnimatorSpeed");
            if (animatorSpeedField != null)
            {
                object floatRef = animatorSpeedField.GetValue(malbersAnimal);
                if (floatRef != null)
                {
                    float newVal = 1f + moveSpeedBonus; // default 1.0 üstüne ekliyoruz
                    var refType = floatRef.GetType();
                    var prop = refType.GetProperty("Value");
                    if (prop != null && prop.CanWrite)
                    {
                        prop.SetValue(floatRef, newVal, null);
                        return;
                    }
                }
            }
        }

        // 2) Animator fallback (root motion varsa bu da hýz etkisi yapar)
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
        if (stats == null || healthStatID == null) return;

        var hp = stats.Stat_Get(healthStatID);
        if (hp == null) return;

        hp.ModifyMAX(addMaxHp);
    }
}
