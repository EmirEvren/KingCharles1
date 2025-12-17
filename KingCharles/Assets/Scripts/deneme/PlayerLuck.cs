using UnityEngine;

public class PlayerLuck : MonoBehaviour
{
    public static PlayerLuck Instance;

    [Min(0)]
    public int luckLevel = 0; // sýnýrsýz (100000 de olabilir). Legendary max %90'a kadar yaklaþýr.

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddLuck(int amount)
    {
        if (amount <= 0) return;
        luckLevel += amount;
    }
}
