using UnityEngine;

public class MiniBossDrops : MonoBehaviour
{
    [Header("Drop Prefabs")]
    public GameObject xpPickupPrefab;
    public GameObject goldPickupPrefab;

    [Header("Totals")]
    public int totalXP = 100;
    public int totalGold = 100;

    [Header("Scatter")]
    public float scatterRadius = 2f;
    public float scatterUp = 0.2f;

    // EnemyHealth içinden çaðýracaðýz (aþaðýda EnemyHealth patch var)
    public void OnEnemyDied()
    {
        DropXP();
        DropGold();
    }

    private void DropXP()
    {
        if (xpPickupPrefab == null || totalXP <= 0) return;

        int each = 1;
        var sample = xpPickupPrefab.GetComponent<XPPickup>();
        if (sample != null) each = Mathf.Max(1, sample.xpAmount);

        int fullCount = totalXP / each;
        int remainder = totalXP % each;

        for (int i = 0; i < fullCount; i++)
            SpawnXP(each);

        if (remainder > 0)
            SpawnXP(remainder);
    }

    private void SpawnXP(int amount)
    {
        Vector2 off = Random.insideUnitCircle * scatterRadius;
        Vector3 pos = transform.position + new Vector3(off.x, scatterUp, off.y);

        GameObject go = Instantiate(xpPickupPrefab, pos, Quaternion.identity);
        var xp = go.GetComponent<XPPickup>();
        if (xp != null) xp.xpAmount = amount;
    }

    private void DropGold()
    {
        if (goldPickupPrefab == null || totalGold <= 0) return;

        int each = 1;
        var sample = goldPickupPrefab.GetComponent<GoldPickup>();
        if (sample != null) each = Mathf.Max(1, sample.goldValue);

        int fullCount = totalGold / each;
        int remainder = totalGold % each;

        for (int i = 0; i < fullCount; i++)
            SpawnGold(each);

        if (remainder > 0)
            SpawnGold(remainder);
    }

    private void SpawnGold(int amount)
    {
        Vector2 off = Random.insideUnitCircle * scatterRadius;
        Vector3 pos = transform.position + new Vector3(off.x, scatterUp, off.y);

        GameObject go = Instantiate(goldPickupPrefab, pos, Quaternion.identity);
        var g = go.GetComponent<GoldPickup>();
        if (g != null) g.goldValue = amount;
    }
}
