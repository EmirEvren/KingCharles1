using UnityEngine;
using Steamworks;
using System;
using System.Collections.Generic;

public class SteamLeaderboardManager : MonoBehaviour
{
    public static SteamLeaderboardManager Instance;

    [Header("Debug Entries (Runtime)")]
    public List<LeaderboardEntryData> entries = new List<LeaderboardEntryData>();

    private SteamLeaderboard_t leaderboard;
    private bool leaderboardReady = false;

    private CallResult<LeaderboardFindResult_t> findResult;
    private CallResult<LeaderboardScoreUploaded_t> uploadResult;
    private CallResult<LeaderboardScoresDownloaded_t> downloadResult;

    private const string LEADERBOARD_NAME = "KILL_LEADERBOARD";

    // UI Event
    public Action OnLeaderboardUpdated;

    // =========================================================
    // INITIALIZE
    // =========================================================
    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        findResult = CallResult<LeaderboardFindResult_t>.Create(OnFound);
        uploadResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnUploaded);
        downloadResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnDownloaded);
    }

    void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("❌ Steam not initialized.");
            return;
        }

        FindLeaderboard();
    }

    // =========================================================
    // FIND LEADERBOARD
    // =========================================================
    void FindLeaderboard()
    {
        var call = SteamUserStats.FindLeaderboard(LEADERBOARD_NAME);
        findResult.Set(call);
    }

    void OnFound(LeaderboardFindResult_t result, bool failure)
    {
        if (failure || result.m_bLeaderboardFound == 0)
        {
            Debug.LogError("❌ Leaderboard bulunamadı.");
            return;
        }

        leaderboard = result.m_hSteamLeaderboard;
        leaderboardReady = true;

        Debug.Log("✅ Leaderboard Ready");

        DownloadTop10();
    }

    // =========================================================
    // UPLOAD SCORE (KEEP BEST)
    // =========================================================
    public void UploadScore(int score)
    {
        if (!leaderboardReady)
        {
            Debug.LogWarning("⚠ Leaderboard not ready");
            return;
        }

        Debug.Log("Uploading score: " + score);

        var call = SteamUserStats.UploadLeaderboardScore(
            leaderboard,
            ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest, // ✅ DEĞİŞTİ
            score,
            null,
            0
        );

        uploadResult.Set(call);
    }

    void OnUploaded(LeaderboardScoreUploaded_t result, bool failure)
    {
        if (failure)
        {
            Debug.LogError("❌ Score upload failed.");
            return;
        }

        if (result.m_bScoreChanged == 1)
            Debug.Log("✅ New High Score Uploaded!");
        else
            Debug.Log("ℹ️ Score was lower than existing. Not updated.");

        DownloadTop10();
    }

    // =========================================================
    // DOWNLOAD TOP 10
    // =========================================================
    public void DownloadTop10()
    {
        if (!leaderboardReady)
        {
            Debug.LogWarning("⚠ Leaderboard not ready yet");
            return;
        }

        Debug.Log("Downloading Top 10...");

        var call = SteamUserStats.DownloadLeaderboardEntries(
            leaderboard,
            ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
            1,
            10
        );

        downloadResult.Set(call);
    }

    void OnDownloaded(LeaderboardScoresDownloaded_t result, bool failure)
    {
        if (failure)
        {
            Debug.LogError("❌ Leaderboard download failed.");
            return;
        }

        entries.Clear();

        for (int i = 0; i < result.m_cEntryCount; i++)
        {
            SteamUserStats.GetDownloadedLeaderboardEntry(
                result.m_hSteamLeaderboardEntries,
                i,
                out LeaderboardEntry_t entry,
                null,
                0
            );

            string name = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);

            entries.Add(new LeaderboardEntryData(
                entry.m_nGlobalRank,
                name,
                entry.m_nScore
            ));
        }

        Debug.Log("✅ Top 10 Updated");

        OnLeaderboardUpdated?.Invoke();
    }
}

// =========================================================
// ENTRY DATA CLASS
// =========================================================
[Serializable]
public class LeaderboardEntryData
{
    public int rank;
    public string steamName;
    public int score;

    public LeaderboardEntryData(int rank, string name, int score)
    {
        this.rank = rank;
        this.steamName = name;
        this.score = score;
    }
}
