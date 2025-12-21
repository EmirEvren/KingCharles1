using UnityEngine;
using TMPro;

public class GameTimerUI : MonoBehaviour
{
    public static GameTimerUI Instance;

    [Header("UI Referansý")]
    public TMP_Text timeText;

    [Header("Ayarlar")]
    public bool startOnAwake = true;

    private float elapsedTime = 0f;
    private bool isRunning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        UpdateTimeText(0f);
    }

    private void Start()
    {
        if (startOnAwake) StartTimer();
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

        timeText.text = $"{minutes:0}:{seconds:00}";
    }

    public void StartTimer() => isRunning = true;
    public void PauseTimer() => isRunning = false;
    public void ResumeTimer() => isRunning = true;

    public void ResetTimer()
    {
        elapsedTime = 0f;
        UpdateTimeText(elapsedTime);
    }

    public float GetElapsedTime() => elapsedTime;
}
