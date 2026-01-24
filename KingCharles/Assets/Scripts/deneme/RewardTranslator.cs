using UnityEngine;
using UnityEngine.Localization;            // Localization kütüphanesi
using UnityEngine.Localization.Components; // UI Componenti için

public class RewardTranslator : MonoBehaviour
{
    // Singleton (WeaponChoiceManager buna ulaşsın diye)
    public static RewardTranslator Instance;

    [Header("UI'daki Çeviri Kutuları (Sürükle)")]
    public LocalizeStringEvent card1TextEvent; // 1. Kartın Textindeki Localize Componenti
    public LocalizeStringEvent card2TextEvent; // 2. Kartın Textindeki Localize Componenti

    [Header("Cebimizdeki Anahtarlar (Inspector'dan Seç)")]
    public LocalizedString tennisBallKey; 
    public LocalizedString fireBallKey;   
    public LocalizedString steakKey;      
    public LocalizedString boneKey;       

    private void Awake()
    {
        // Singleton kurulumu
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // WeaponChoiceManager bu fonksiyonu çağıracak
    public void UpdateCardTexts(WeaponType weapon1, WeaponType weapon2)
    {
        // 1. Kartın Key'ini değiştiriyoruz
        if (card1TextEvent != null)
        {
            card1TextEvent.StringReference = GetKeyByWeapon(weapon1);
            card1TextEvent.gameObject.SetActive(true);
        }

        // 2. Kartın Key'ini değiştiriyoruz
        if (card2TextEvent != null)
        {
            card2TextEvent.StringReference = GetKeyByWeapon(weapon2);
            card2TextEvent.gameObject.SetActive(true);
        }
    }

    // Gelen silaha göre doğru anahtarı bulan yardımcı fonksiyon
    private LocalizedString GetKeyByWeapon(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.TennisBall: return tennisBallKey;
            case WeaponType.Fireball:   return fireBallKey;
            case WeaponType.Steak:      return steakKey;
            case WeaponType.Bone:       return boneKey;
            default: return tennisBallKey; 
        }
    }
}