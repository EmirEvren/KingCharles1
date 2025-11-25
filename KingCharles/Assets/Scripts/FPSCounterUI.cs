using UnityEngine;
using TMPro; // TextMeshPro kütüphanesi şart

public class FPSCounterUI : MonoBehaviour
{
    public TextMeshProUGUI fpsText; // Paneldeki Text'i buraya atacağız
    public float updateInterval = 0.2f; // Ne sıklıkla güncellensin (0.2sn iyidir)

    private float accum = 0; // Kare süresi toplamı
    private int frames = 0; // Kare sayısı
    private float timeleft; // Güncelleme için kalan süre

    void Start()
    {
        timeleft = updateInterval;
    }

    void Update()
    {
        timeleft -= Time.deltaTime;
        accum += Time.timeScale / Time.deltaTime;
        ++frames;

        // Süre dolunca ekrana yaz
        if (timeleft <= 0.0)
        {
            float fps = accum / frames;
            string format = System.String.Format("{0:F0} FPS", fps);
            
            if(fpsText != null)
            {
                fpsText.text = format;

                // Renk değişimi (İstersen kullan)
                if (fps < 30) fpsText.color = Color.red;
                else if (fps < 60) fpsText.color = Color.yellow;
                else fpsText.color = Color.green;
            }

            timeleft = updateInterval;
            accum = 0.0f;
            frames = 0;
        }
    }
}