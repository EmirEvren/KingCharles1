using UnityEngine;
using UnityEngine.UI;

public class WeaponHUDIcons : MonoBehaviour
{
    public static WeaponHUDIcons Instance;

    [Header("HUD Slotları")]
    public Image firstWeaponImage;   // 1. seçilen silah
    public Image secondWeaponImage;  // 2. seçilen silah

    private bool firstFilled = false;
    private bool secondFilled = false;

    private void Awake()
    {
        Instance = this;

        // Başlangıçta ikisi de kapalı
        if (firstWeaponImage != null)
        {
            firstWeaponImage.enabled = false;
            firstWeaponImage.sprite = null;
        }

        if (secondWeaponImage != null)
        {
            secondWeaponImage.enabled = false;
            secondWeaponImage.sprite = null;
        }
    }

    /// <summary>
    /// WeaponChoiceManager, yeni bir silah alındığında burayı çağırıyor.
    /// type → alınan silahın WeaponType'ı
    /// </summary>
    public void OnWeaponAcquired(WeaponType type)
    {
        if (WeaponChoiceManager.Instance == null)
        {
            Debug.LogWarning("[WeaponHUDIcons] WeaponChoiceManager.Instance yok.");
            return;
        }

        // Silaha ait WeaponOption'u bul ve icon'unu al
        WeaponOption opt = WeaponChoiceManager.Instance.GetWeaponOption(type);
        if (opt == null || opt.icon == null)
        {
            Debug.LogWarning($"[WeaponHUDIcons] {type} için WeaponOption veya icon bulunamadı.");
            return;
        }

        Sprite icon = opt.icon;

        // 1. slot boşsa → buraya koy
        if (!firstFilled && firstWeaponImage != null)
        {
            firstFilled = true;
            firstWeaponImage.sprite = icon;
            firstWeaponImage.enabled = true;
            return;
        }

        // 2. slot boşsa → buraya koy
        if (!secondFilled && secondWeaponImage != null)
        {
            secondFilled = true;
            secondWeaponImage.sprite = icon;
            secondWeaponImage.enabled = true;
            return;
        }

        // İki slot doluysa şimdilik hiçbir şey yapmıyoruz.
        // (İleride istersen swap/replace mantığı ekleriz.)
    }
}
