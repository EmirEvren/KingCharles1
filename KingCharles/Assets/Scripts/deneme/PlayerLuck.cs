using UnityEngine;

public class PlayerLuck : MonoBehaviour
{
    public static PlayerLuck Instance;

    // Sýnýrsýz: 0, 1, 2, 3 ... 100000 ...
    public int luckLevel = 0;

    // 0..1'e yumuþak dönüþüm:
    // luck büyüdükçe 1'e yaklaþýr ama "garanti %100 Legendary" olmaz.
    // WeaponChoiceManager zaten luck01=1 iken Legendary'yi %90'a kadar çýkarýyor.
    public float Luck01
    {
        get
        {
            int l = Mathf.Max(0, luckLevel);
            return 1f - Mathf.Exp(-l / 15f);
        }
    }

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
