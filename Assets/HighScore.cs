using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class HighScore : MonoBehaviour
{
    static private TextMeshProUGUI uiText;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        RectTransform rect = GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300f, 240f);
        uiText.fontSize = 22f;
        uiText.alignment = TextAlignmentOptions.TopLeft;
        RefreshDisplay();
    }

    public static void RefreshDisplay()
    {
        if (uiText == null) return;
        var builder = new StringBuilder("Leaderboard\n");
        List<LeaderboardEntry> entries = LeaderboardStore.GetEntries();
        if (entries.Count == 0)
        {
            builder.AppendLine("No scores yet");
        }
        else
        {
            for (int i = 0; i < entries.Count; i++)
            {
                LeaderboardEntry entry = entries[i];
                builder.AppendLine($"{i + 1}. {entry.name} - {entry.score:N0}");
            }
        }

        if (LeaderboardStore.HasPlayerName)
            builder.AppendLine().Append("Playing as ").Append(LeaderboardStore.PlayerName);
        uiText.text = builder.ToString();
    }

    [Tooltip("Check this box to clear saved leaderboard scores in PlayerPrefs")]
    public bool resetLeaderboardNow = false;

    void OnDrawGizmos()
    {
        if (!resetLeaderboardNow) return;
        resetLeaderboardNow = false;
        PlayerPrefs.DeleteKey("LeaderboardData");
        PlayerPrefs.Save();
        Debug.LogWarning("Leaderboard cleared from PlayerPrefs.");
    }
}
