using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Lives on the Canvas. Finds modal children by name and wires buttons in Awake.
public class GameUIManager : MonoBehaviour
{
    public event Action<string> NameSubmitted;
    public event Action ShieldAccepted;
    public event Action RestartRequested;
    public event Action NewGameRequested;

    public bool IsNamePromptOpen { get; private set; }
    public bool IsShieldPromptOpen { get; private set; }
    public bool IsGameOverOpen { get; private set; }
    public bool BlocksGameplay => IsNamePromptOpen || IsShieldPromptOpen || IsGameOverOpen;

    private GameObject startModal;
    private GameObject shieldModal;
    private GameObject gameOverModal;
    private GameObject backdropModal;

    private TMP_InputField nameField;
    private Button startButton;
    private Button shieldButton;
    private Button restartButton;
    private Button newGameButton;
    private TMP_Text gameOverBody;

    void Awake()
    {
        startModal = FindModal("StartModal");
        shieldModal = FindModal("ShieldModal");
        gameOverModal = FindModal("GameOverModal");
        backdropModal = FindModal("ModalBackdrop");

        if (startModal != null)
        {
            nameField = startModal.GetComponentInChildren<TMP_InputField>(true);
            startButton = startModal.GetComponentInChildren<Button>(true);
            if (startButton != null) startButton.onClick.AddListener(StartRun);
        }

        if (shieldModal != null)
        {
            shieldButton = shieldModal.GetComponentInChildren<Button>(true);
            if (shieldButton != null) shieldButton.onClick.AddListener(() => ShieldAccepted?.Invoke());
        }

        if (gameOverModal != null)
        {
            Transform notice = gameOverModal.transform.Find("Notice");
            if (notice != null) gameOverBody = notice.GetComponent<TMP_Text>();

            Transform restart = gameOverModal.transform.Find("RestartButton");
            if (restart == null)
            {
                foreach (Button button in gameOverModal.GetComponentsInChildren<Button>(true))
                {
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null && label.text.IndexOf("restart", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        restartButton = button;
                        break;
                    }
                }
            }
            else restartButton = restart.GetComponent<Button>();

            Transform newGame = gameOverModal.transform.Find("NewGameButton");
            if (newGame != null) newGameButton = newGame.GetComponent<Button>();
            else
            {
                foreach (Button button in gameOverModal.GetComponentsInChildren<Button>(true))
                {
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                    if (label != null && label.text.IndexOf("new game", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        newGameButton = button;
                        break;
                    }
                }
            }

            if (restartButton != null) restartButton.onClick.AddListener(RestartGame);
            if (newGameButton != null) newGameButton.onClick.AddListener(NewGame);
        }

        HideAllModals();
        SyncBackdrop();
    }

    void Start()
    {
        if (!LeaderboardStore.IsRunActive)
            ShowNameEntry();
    }

    private GameObject FindModal(string modalName)
    {
        Transform modal = transform.Find(modalName);
        return modal != null ? modal.gameObject : null;
    }

    private void HideAllModals()
    {
        if (startModal != null) startModal.SetActive(false);
        if (shieldModal != null) shieldModal.SetActive(false);
        if (gameOverModal != null) gameOverModal.SetActive(false);
        IsNamePromptOpen = false;
        IsShieldPromptOpen = false;
        IsGameOverOpen = false;
        SyncBackdrop();
    }

    private void SyncBackdrop()
    {
        if (backdropModal == null) return;
        bool show = (startModal != null && startModal.activeSelf)
            || (shieldModal != null && shieldModal.activeSelf)
            || (gameOverModal != null && gameOverModal.activeSelf);
        backdropModal.SetActive(show);
    }

    public void ShowNameEntry()
    {
        if (startModal == null) return;
        HideAllModals();
        IsNamePromptOpen = true;
        Pause();
        startModal.SetActive(true);
        SyncBackdrop();
        if (nameField != null)
        {
            nameField.text = LeaderboardStore.HasPlayerName ? LeaderboardStore.PlayerName : string.Empty;
            nameField.ActivateInputField();
        }
    }

    private void StartRun()
    {
        string name = nameField != null ? nameField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) return;
        LeaderboardStore.SetPlayerName(name);
        LeaderboardStore.MarkRunActive();
        Leaderboard.RefreshDisplay();
        NameSubmitted?.Invoke(name);
        ReloadScene();
    }

    public void ShowShieldUnlock()
    {
        if (shieldModal == null) return;
        IsShieldPromptOpen = true;
        Pause();
        shieldModal.SetActive(true);
        SyncBackdrop();
    }

    public void HideShieldUnlock()
    {
        if (shieldModal != null) shieldModal.SetActive(false);
        IsShieldPromptOpen = false;
        SyncBackdrop();
        Resume();
    }

    public void ShowGameOver(string title, string description)
    {
        if (gameOverModal == null) return;
        if (startModal != null) startModal.SetActive(false);
        if (shieldModal != null) shieldModal.SetActive(false);
        IsNamePromptOpen = false;
        IsShieldPromptOpen = false;
        IsGameOverOpen = true;
        Pause();
        if (gameOverBody != null)
            gameOverBody.text = string.IsNullOrEmpty(description) ? title : title + "\n" + description;
        gameOverModal.SetActive(true);
        SyncBackdrop();
    }

    public void Pause()
    {
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        Time.timeScale = 1f;
    }

    public void RestartGame()
    {
        RestartRequested?.Invoke();
        ReloadScene();
    }

    public void NewGame()
    {
        NewGameRequested?.Invoke();
        LeaderboardStore.ClearPlayerName();
        LeaderboardStore.ClearRunActive();
        ShowNameEntry();
    }

    private static void ReloadScene()
    {
        Time.timeScale = 1f;
        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.buildIndex);
    }

    void OnDestroy()
    {
        if (BlocksGameplay) Time.timeScale = 1f;
    }
}
