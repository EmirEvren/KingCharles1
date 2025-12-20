using UnityEngine;

public class ChestPricingManager : MonoBehaviour
{
    public static ChestPricingManager Instance;

    [Header("Pricing")]
    public int basePrice = 30;

    // Global state
    [SerializeField] private int openedCount = 0;
    [SerializeField] private int totalSpent = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int GetCurrentPrice()
    {
        // 1. açýlýþ: 30
        if (openedCount == 0) return basePrice;

        // 2. açýlýþ: 60
        if (openedCount == 1) return basePrice * 2;

        // 3. ve sonrasý: önceki harcananlarýn toplamý (90, 180, 360, ...)
        return Mathf.Max(basePrice, totalSpent);
    }

    public void RegisterOpened(int paidPrice)
    {
        openedCount++;
        totalSpent += Mathf.Max(0, paidPrice);
    }
}
