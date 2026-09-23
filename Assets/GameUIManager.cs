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
    private float resumeTimeScale = 1f;

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
            if (startButton != null) startButton.onClick.AddListener(SubmitName);
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
            foreach (Button button in gameOverModal.GetComponentsInChildren<Button>(true))
            {
                TMP_Text label = button.GetComponentInChildren<TMP_Text>();
                if (label == null) continue;
                if (label.text == "Restart") restartButton = button;
                else if (label.text == "New Game") newGameButton = button;
            }
            if (restartButton != null) restartButton.onClick.AddListener(() => RestartRequested?.Invoke());
            if (newGameButton != null) newGameButton.onClick.AddListener(() => NewGameRequested?.Invoke());
        }

        HideAllModals();
        SyncBackdrop();
    }

    void Start()
    {
        if (!LeaderboardStore.HasPlayerName)
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
            nameField.text = string.Empty;
            nameField.ActivateInputField();
        }
    }

    private void SubmitName()
    {
        string name = nameField != null ? nameField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) return;
        LeaderboardStore.SetPlayerName(name);
        IsNamePromptOpen = false;
        startModal.SetActive(false);
        SyncBackdrop();
        Leaderboard.RefreshDisplay();
        Resume();
        NameSubmitted?.Invoke(name);
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
        if (Time.timeScale > 0f) resumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    public void Resume()
    {
        Time.timeScale = resumeTimeScale > 0f ? resumeTimeScale : 1f;
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("_Scene_0");
    }

    public void NewGame()
    {
        LeaderboardStore.ClearPlayerName();
        RestartGame();
    }

    void OnDestroy()
    {
        if (BlocksGameplay) Time.timeScale = 1f;
    }
}
