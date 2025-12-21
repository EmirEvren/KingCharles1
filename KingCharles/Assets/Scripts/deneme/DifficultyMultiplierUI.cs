using UnityEngine;
using TMPro;

public class DifficultyMultiplierUI : MonoBehaviour
{
    [Header("UI")]
    public TMP_Text difficultyText;

    [Header("Format")]
    public string prefix = "Difficulty: x";
    public int decimals = 2;

    [Header("Update")]
    public float updateInterval = 0.1f;

    private float timer;

    private void Start()
    {
        UpdateText();
    }

    private void Update()
    {
        timer += Time.unscaledDeltaTime; // oyun durunca da (Time.timeScale=0) UI güncellenebilsin
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdateText();
        }
    }

    private void UpdateText()
    {
        if (difficultyText == null) return;

        float mul = 1f;
        if (RunDifficultyManager.Instance != null)
            mul = RunDifficultyManager.Instance.GetCurrentMultiplier();

        // Örn: Difficulty: x1.44
        difficultyText.text = $"{prefix}{mul.ToString($"F{decimals}")}";
    }
}
