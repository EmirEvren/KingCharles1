using System.Collections.Generic;
using UnityEngine;

public class IceBreathDamage : MonoBehaviour
{
    [Header("Damage")]
    public float damagePerTick = 500f;
    public float tickInterval = 1f;

    [Header("Filter")]
    public string enemyTag = "Enemy";

    // Aynı enemy'nin birden fazla collider'ı olabiliyor:
    // enter/exit dengesi bozulmasın diye sayaç tutuyoruz.
    private readonly Dictionary<EnemyHealth, int> overlapCounts = new Dictionary<EnemyHealth, int>(64);

    private float tickTimer = 0f;

    // ✅ EKLENDİ: GlobalIceBreath buradan damage / interval set edebilsin
    public void Setup(float damage, float interval)
    {
        damagePerTick = damage;

        // 0 olmasın diye güvenlik
        tickInterval = Mathf.Max(0.01f, interval);

        // Yeni değerlerle düzgün tick için timer reset
        tickTimer = 0f;
    }

    private void OnEnable()
    {
        tickTimer = 0f;
        overlapCounts.Clear();
    }

    private void OnDisable()
    {
        overlapCounts.Clear();
    }

    private void Update()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer < tickInterval) return;

        // 1 saniyelik tick'leri kaçırmamak için while (lag olursa birikmesin diye)
        while (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;

            // Tick: içeride kalan herkes 500 yesin
            // null temizliği de yapıyoruz
            var keys = ListPool.GetKeys(overlapCounts);

            for (int i = keys.Count - 1; i >= 0; i--)
            {
                EnemyHealth eh = keys[i];
                if (eh == null || !eh.gameObject.activeInHierarchy)
                {
                    overlapCounts.Remove(eh);
                    continue;
                }

                eh.TakeDamage(damagePerTick);
            }

            ListPool.Release(keys);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        // Tag filtresi (EnemyHealth parent’ta olabilir)
        if (!string.IsNullOrEmpty(enemyTag))
        {
            // child collider -> root tag kontrolü de yap
            bool tagOk = other.CompareTag(enemyTag) || (other.transform.root != null && other.transform.root.CompareTag(enemyTag));
            if (!tagOk)
            {
                // tag tutmadıysa yine de EnemyHealth arayacağız (bazı prefablar tag'i child'a koyabiliyor)
            }
        }

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh == null) eh = other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        if (overlapCounts.TryGetValue(eh, out int c))
            overlapCounts[eh] = c + 1;
        else
            overlapCounts[eh] = 1;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        EnemyHealth eh = other.GetComponent<EnemyHealth>();
        if (eh == null) eh = other.GetComponentInParent<EnemyHealth>();
        if (eh == null) return;

        if (!overlapCounts.TryGetValue(eh, out int c)) return;

        c--;
        if (c <= 0) overlapCounts.Remove(eh);
        else overlapCounts[eh] = c;
    }

    // Küçük helper: Dictionary key'lerini alloc yapmadan almak için basit pool
    private static class ListPool
    {
        private static readonly Stack<List<EnemyHealth>> pool = new Stack<List<EnemyHealth>>(8);

        public static List<EnemyHealth> GetKeys(Dictionary<EnemyHealth, int> dict)
        {
            List<EnemyHealth> list = (pool.Count > 0) ? pool.Pop() : new List<EnemyHealth>(64);
            list.Clear();
            foreach (var kv in dict) list.Add(kv.Key);
            return list;
        }

        public static void Release(List<EnemyHealth> list)
        {
            if (list == null) return;
            list.Clear();
            pool.Push(list);
        }
    }
}