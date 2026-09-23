using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LeaderboardEntry
{
    public string name;
    public int score;
}

[Serializable]
public class LeaderboardData
{
    public List<LeaderboardEntry> entries = new List<LeaderboardEntry>();
}

public static class LeaderboardStore
{
    const string PlayerNameKey = "PlayerName";
    const string RunActiveKey = "RunActive";
    const string DataKey = "LeaderboardData";

    public static int MaxEntries { get; set; } = 8;

    public static string PlayerName => PlayerPrefs.GetString(PlayerNameKey, string.Empty);

    public static bool HasPlayerName => !string.IsNullOrWhiteSpace(PlayerName);

    public static bool IsRunActive => PlayerPrefs.GetInt(RunActiveKey, 0) == 1;

    public static void MarkRunActive()
    {
        PlayerPrefs.SetInt(RunActiveKey, 1);
        PlayerPrefs.Save();
    }

    public static void ClearRunActive()
    {
        PlayerPrefs.DeleteKey(RunActiveKey);
        PlayerPrefs.Save();
    }

    public static void SetPlayerName(string name)
    {
        PlayerPrefs.SetString(PlayerNameKey, name.Trim());
        PlayerPrefs.Save();
    }

    public static void ClearPlayerName()
    {
        PlayerPrefs.DeleteKey(PlayerNameKey);
        PlayerPrefs.Save();
    }

    public static List<LeaderboardEntry> GetEntries()
    {
        if (!PlayerPrefs.HasKey(DataKey)) return new List<LeaderboardEntry>();
        string json = PlayerPrefs.GetString(DataKey, string.Empty);
        if (string.IsNullOrEmpty(json)) return new List<LeaderboardEntry>();
        LeaderboardData data = JsonUtility.FromJson<LeaderboardData>(json);
        return data?.entries ?? new List<LeaderboardEntry>();
    }

    public static void AddScore(string name, int score)
    {
        if (string.IsNullOrWhiteSpace(name) || score <= 0) return;
        List<LeaderboardEntry> entries = GetEntries();
        entries.Add(new LeaderboardEntry { name = name.Trim(), score = score });
        entries.Sort((a, b) => b.score.CompareTo(a.score));
        if (entries.Count > MaxEntries)
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        SaveEntries(entries);
    }

    public static int GetTopScore()
    {
        List<LeaderboardEntry> entries = GetEntries();
        return entries.Count > 0 ? entries[0].score : 0;
    }

    private static void SaveEntries(List<LeaderboardEntry> entries)
    {
        string json = JsonUtility.ToJson(new LeaderboardData { entries = entries });
        PlayerPrefs.SetString(DataKey, json);
        PlayerPrefs.Save();
    }
}
