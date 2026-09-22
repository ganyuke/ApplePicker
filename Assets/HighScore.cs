using TMPro;
using UnityEngine;

public class HighScore : MonoBehaviour
{
    static private TextMeshProUGUI _UI_TEXT;
    static private int _SCORE = 1000;

    void Awake()
    {
        _UI_TEXT = GetComponent<TextMeshProUGUI>();
        if (PlayerPrefs.HasKey("HighScore"))
            SCORE = PlayerPrefs.GetInt("HighScore");
        PlayerPrefs.SetInt("HighScore", SCORE);
    }

    public static int SCORE
    {
        get => _SCORE;
        private set
        {
            _SCORE = value;
            PlayerPrefs.SetInt("HighScore", value);
            if (_UI_TEXT != null)
                _UI_TEXT.text = "High Score: " + value.ToString("#,0");
        }
    }

    public static void TRY_SET_HIGH_SCORE(int scoreToTry)
    {
        if (scoreToTry <= SCORE) return;
        SCORE = scoreToTry;
    }

    [Tooltip("Check this box to reset the HighScore in PlayerPrefs")]
    public bool resetHighScoreNow = false;

    void OnDrawGizmos()
    {
        if (!resetHighScoreNow) return;
        resetHighScoreNow = false;
        PlayerPrefs.SetInt("HighScore", 1000);
        Debug.LogWarning("PlayerPrefs HighScore reset to 1,000.");
    }
}
