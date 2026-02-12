using UnityEngine;

public class LeaderboardUI : MonoBehaviour
{
    public Transform contentParent;
    public LeaderboardEntryUI rowPrefab;

    private void OnEnable()
    {
        if (SteamLeaderboardManager.Instance != null)
        {
            SteamLeaderboardManager.Instance.OnLeaderboardUpdated += Refresh;
            Refresh(); // Panel açıldığında mevcut veriyi de göster
        }
    }

    private void OnDisable()
    {
        if (SteamLeaderboardManager.Instance != null)
        {
            SteamLeaderboardManager.Instance.OnLeaderboardUpdated -= Refresh;
        }
    }

    public void Refresh()
    {
        if (SteamLeaderboardManager.Instance == null)
            return;

        // Eski satırları temizle
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        var data = SteamLeaderboardManager.Instance.entries;

        for (int i = 0; i < data.Count; i++)
        {
            var entry = data[i];
            var row = Instantiate(rowPrefab, contentParent);
            row.Set(entry.rank, entry.steamName, entry.score);
        }
    }
}
