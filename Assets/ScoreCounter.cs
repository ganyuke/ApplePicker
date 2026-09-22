using System;
using TMPro;
using UnityEngine;

public class ScoreCounter : MonoBehaviour
{
    [Header("Dynamic")] public int score = 0;
    private TextMeshProUGUI uiText;
    private int lastNotifiedScore;
    public event Action<int> ScoreChanged;

    void Awake()
    {
        uiText = GetComponent<TextMeshProUGUI>();
        lastNotifiedScore = score;
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RefreshScore();
    }

    // Update is called once per frame
    void Update()
    {
        // Also support changes made directly to the existing public score field.
        if (score != lastNotifiedScore) RefreshScore();
    }

    public void AddPoints(int points)
    {
        score = (int)Math.Max(0L, Math.Min(int.MaxValue, (long)score + points));
        RefreshScore();
    }

    private void RefreshScore()
    {
        if (uiText != null) uiText.text = score.ToString("#,0");
        if (score == lastNotifiedScore) return;
        lastNotifiedScore = score;
        ScoreChanged?.Invoke(score);
    }
}
