using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour
{
    [Header("UI Referansý")]
    public TMP_Text timeText;      // Inspector'dan atayacaðýn TMP Text

    [Header("Ayarlar")]
    public bool startOnAwake = true;   // Oyun baþlar baþlamaz çalýþsýn mý?

    private float elapsedTime = 0f;
    private bool isRunning = false;

    private void Awake()
    {
        // Baþlangýçta 0:00 yaz
        UpdateTimeText(0f);
    }

    private void Start()
    {
        if (startOnAwake)
        {
            StartTimer();
        }
    }

    private void Update()
    {
        if (!isRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimeText(elapsedTime);
    }

    private void UpdateTimeText(float time)
    {
        if (timeText == null) return;

        int totalSeconds = Mathf.FloorToInt(time);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;

        // 00:00 formatýnda yaz
        timeText.text = $"{minutes:0}:{seconds:00}";
    }

    // ---- Dýþarýdan çaðýrabileceðin fonksiyonlar ----

    public void StartTimer()
    {
        isRunning = true;
    }

    public void PauseTimer()
    {
        isRunning = false;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimeText(elapsedTime);
    }

    // Ýstersen baþka scriptlerden süreyi okuyabil
    public float GetElapsedTime()
    {
        return elapsedTime;
    }
}
