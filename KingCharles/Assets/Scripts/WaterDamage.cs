using UnityEngine;
using System.Collections;
using MalbersAnimations; // Malbers sistemine erişmek için gerekli

public class WaterDamage : MonoBehaviour
{
    [Header("Hasar Ayarları")]
    [Tooltip("Saniyede ne kadar can gidecek?")]
    public float saniyelikHasar = 10f; 
    
    [Tooltip("Azalacak stat'ın adı (Malbers Stats içinde yazan isim)")]
    public string statAdi = "Health"; 

    // Hasar döngüsünü tutacağımız değişken
    private Coroutine hasarRoutinesi;

    // Oyuncu suya girdiğinde
    private void OnTriggerEnter(Collider other)
    {
        // Giren obje "Animal" etiketine sahipse
        if (other.CompareTag("Animal"))
        {
            // Oyuncudaki Malbers Stats bileşenini bul (Collider alt objelerde olabileceği için InParent kullanıyoruz)
            Stats animalStats = other.GetComponentInParent<Stats>(); 
            
            if (animalStats != null)
            {
                // Hasar döngüsünü başlat
                hasarRoutinesi = StartCoroutine(SaniyedeBirHasarVer(animalStats));
            }
        }
    }

    // Oyuncu sudan çıktığında
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Animal"))
        {
            // Eğer döngü çalışıyorsa durdur (artık hasar almasın)
            if (hasarRoutinesi != null)
            {
                StopCoroutine(hasarRoutinesi);
                hasarRoutinesi = null;
            }
        }
    }

    // Her saniye hasar veren döngü
    private IEnumerator SaniyedeBirHasarVer(Stats stats)
    {
        while (true) // Sudan çıkana kadar sonsuz döner
        {
            // Malbers sisteminde can düşürmek için eksi (-) değer yolluyoruz
            stats.Stat_ModifyValue(statAdi, -saniyelikHasar);
            
            // 1 saniye bekle
            yield return new WaitForSeconds(1f);
        }
    }
}