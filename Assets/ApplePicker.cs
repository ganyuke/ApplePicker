using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class ApplePicker : MonoBehaviour
{
    [Header("Basket Rig")]
    public Basket basketRig;
    public Transform basketBottomsParent;
    public GameObject basketBottomPrefab;
    public int numBaskets = 3;
    public float basketBottomY = -14f;
    public float basketSpacingY = 2f;
    public List<GameObject> basketList;

    [Header("Shielding (scene objects under Basket)")]
    public GameObject shieldsRoot;
    public Transform shieldLeft;
    public Transform shieldRight;

    [Header("Rewards")]
    public int normalApplePoints = 100;
    public int goldenApplePoints = 500;
    [Min(1)] public int firstBasketRewardScore = 5000;
    [Min(1)] public int shieldingScore = 5000;

    [Header("Shielding")]
    [Min(1f)] public float basketMoveSpeed = 60f;
    [Min(0.1f)] public float shieldThickness = 0.35f;
    [Min(0f)] public float shieldTopExtension = 1.5f;
    public Vector3 padSize = new Vector3(4f, 0.5f, 4f);
    [Range(0f, 60f)] public float padAngle = 25f;
    [Range(0f, 1f)] public float padAimStrength = 0.65f;
    public Color shieldColor = new Color(0.2f, 0.85f, 1f);
    public Color padColor = new Color(1f, 0.55f, 0.15f);
    public ParticleSystem shieldHitEffectPrefab;

    public bool ShieldingUnlocked { get; private set; }
    public bool IsShieldPromptOpen { get; private set; }
    public bool IsNamePromptOpen { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPlaying => !IsShieldPromptOpen && !IsNamePromptOpen && !IsGameOver && Time.timeScale > 0f;
    public float BasketX { get; private set; }
    public long NextBasketRewardScore { get; private set; }

    private ScoreCounter scoreCounter;
    private float targetBasketX;
    private int lastLossFrame = -1;
    private GameObject modal;
    private PhysicsMaterial bounceMaterial;
    private GameObject padsRoot;
    private float resumeTimeScale = 1f;

    void Start()
    {
        basketList = new List<GameObject>();
        if (basketRig == null) basketRig = FindAnyObjectByType<Basket>();
        if (basketBottomsParent == null && basketRig != null)
            basketBottomsParent = basketRig.transform.Find("BasketBottoms");
        if (shieldsRoot == null && basketRig != null)
        {
            Transform shields = basketRig.transform.Find("Shields");
            if (shields != null) shieldsRoot = shields.gameObject;
        }
        if (shieldLeft == null && shieldsRoot != null)
            shieldLeft = shieldsRoot.transform.Find("ShieldLeft");
        if (shieldRight == null && shieldsRoot != null)
            shieldRight = shieldsRoot.transform.Find("ShieldRight");
        if (shieldsRoot != null) shieldsRoot.SetActive(false);

        NextBasketRewardScore = Mathf.Max(1, firstBasketRewardScore);
        PositionBasketRig();
        for (int i = 0; i < numBaskets; i++) RestoreBasket();
        scoreCounter = FindAnyObjectByType<ScoreCounter>();
        if (scoreCounter != null)
        {
            scoreCounter.ScoreChanged += OnScoreChanged;
            OnScoreChanged(scoreCounter.score);
        }

        if (!LeaderboardStore.HasPlayerName)
            ShowNameEntryModal();
    }

    void FixedUpdate()
    {
        if (!IsPlaying) return;
        BasketX = Mathf.MoveTowards(BasketX, ClampBasketX(targetBasketX), basketMoveSpeed * Time.fixedDeltaTime);
        // Shields are children of the basket rig and move with it automatically.
    }

    public void SetBasketTargetX(float x)
    {
        if (IsPlaying) targetBasketX = ClampBasketX(x);
    }

    private float BasketHalfWidth => basketBottomPrefab.transform.localScale.x *
        basketBottomPrefab.GetComponent<BoxCollider>().size.x * 0.5f;

    private float BasketBottomHeight => basketBottomPrefab.transform.localScale.y *
        basketBottomPrefab.GetComponent<BoxCollider>().size.y;

    private float ClampBasketX(float x)
    {
        Camera camera = Camera.main;
        if (camera == null) return x;
        float halfWidth = camera.orthographicSize * camera.aspect;
        float margin = BasketHalfWidth + (ShieldingUnlocked ? shieldThickness : 0f) + 0.1f;
        float travel = Mathf.Max(0f, halfWidth - margin);
        return Mathf.Clamp(x, camera.transform.position.x - travel, camera.transform.position.x + travel);
    }

    private void OnScoreChanged(int score)
    {
        if (IsGameOver) return;
        while (score >= NextBasketRewardScore)
        {
            RestoreBasket();
            NextBasketRewardScore *= 2L;
        }
        if (score >= shieldingScore && !ShieldingUnlocked && !IsShieldPromptOpen)
        {
            IsShieldPromptOpen = true;
            PauseGame();
            ShowShieldUnlockModal();
        }
    }

    public bool RestoreBasket()
    {
        if (basketList.Count >= numBaskets || basketBottomPrefab == null || basketBottomsParent == null) return false;
        float localY = basketSpacingY * basketList.Count;
        GameObject bottom = Instantiate(basketBottomPrefab, basketBottomsParent);
        bottom.transform.localPosition = new Vector3(0f, localY, 0f);
        bottom.transform.localRotation = Quaternion.identity;
        basketList.Add(bottom);
        if (ShieldingUnlocked) UpdateShields();
        return true;
    }

    private void PositionBasketRig()
    {
        if (basketRig == null) return;
        Rigidbody body = basketRig.GetComponent<Rigidbody>();
        Vector3 pos = new Vector3(BasketX, basketBottomY, 0f);
        if (body != null) body.position = pos;
        else basketRig.transform.position = pos;
    }

    public void AppleMissed()
    {
        if (!IsPlaying || basketList.Count == 0 || lastLossFrame == Time.frameCount) return;
        lastLossFrame = Time.frameCount;
        GameObject[] appleArray = GameObject.FindGameObjectsWithTag("Apple");
        foreach (GameObject tempGO in appleArray)
        {
            Apple apple = tempGO.GetComponent<Apple>();
            if (apple != null) apple.TryConsume();
            else Destroy(tempGO);
        }
        int basketIndex = basketList.Count - 1;
        GameObject bottom = basketList[basketIndex];
        basketList.RemoveAt(basketIndex);
        Destroy(bottom);
        if (basketList.Count == 0)
            ShowGameOverModal();
        else if (ShieldingUnlocked) UpdateShields();
    }

    public void ActivateShielding()
    {
        if (!IsShieldPromptOpen || IsGameOver || ShieldingUnlocked) return;
        ShieldingUnlocked = true;
        EnsureBounceMaterial();
        ConfigureShield(shieldLeft);
        ConfigureShield(shieldRight);
        if (shieldsRoot != null) shieldsRoot.SetActive(true);
        BasketX = ClampBasketX(BasketX);
        targetBasketX = BasketX;
        PositionBasketRig();
        UpdateShields();
        padsRoot = new GameObject("Bounce Pads");
        CreatePad(-1f);
        CreatePad(1f);
        IsShieldPromptOpen = false;
        if (modal != null) Destroy(modal);
        Time.timeScale = resumeTimeScale;
    }

    private void EnsureBounceMaterial()
    {
        if (bounceMaterial != null) return;
        bounceMaterial = new PhysicsMaterial("Shield and pad bounce")
        {
            bounciness = 1f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
    }

    private void ConfigureShield(Transform shield)
    {
        if (shield == null) return;
        Collider collider = shield.GetComponent<Collider>();
        if (collider != null) collider.sharedMaterial = bounceMaterial;
        shield.gameObject.layer = LayerMask.NameToLayer("Basket");
        Renderer renderer = shield.GetComponent<Renderer>();
        if (renderer != null)
        {
            var properties = new MaterialPropertyBlock();
            properties.SetColor("_BaseColor", shieldColor);
            properties.SetColor("_Color", shieldColor);
            renderer.SetPropertyBlock(properties);
        }
        AppleBounceSurface bounceSurface = shield.GetComponent<AppleBounceSurface>();
        if (bounceSurface == null) bounceSurface = shield.gameObject.AddComponent<AppleBounceSurface>();
        bounceSurface.isShield = true;
        bounceSurface.padAimStrength = padAimStrength;
        bounceSurface.shieldHitEffectPrefab = shieldHitEffectPrefab;
    }

    private GameObject CreateBounceSurface(string surfaceName, Color color, bool isShield)
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = surfaceName;
        surface.transform.SetParent(padsRoot.transform);
        surface.layer = LayerMask.NameToLayer("Basket");
        surface.GetComponent<Collider>().sharedMaterial = bounceMaterial;
        Renderer surfaceRenderer = surface.GetComponent<Renderer>();
        if (basketBottomPrefab != null)
            surfaceRenderer.sharedMaterial = basketBottomPrefab.GetComponent<Renderer>().sharedMaterial;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        surfaceRenderer.SetPropertyBlock(properties);
        AppleBounceSurface bounceSurface = surface.AddComponent<AppleBounceSurface>();
        bounceSurface.isShield = isShield;
        bounceSurface.padAimStrength = padAimStrength;
        bounceSurface.shieldHitEffectPrefab = shieldHitEffectPrefab;
        return surface;
    }

    private void CreatePad(float side)
    {
        Camera camera = Camera.main;
        float halfWidth = camera.orthographicSize * camera.aspect;
        GameObject pad = CreateBounceSurface(side < 0 ? "Left Bounce Pad" : "Right Bounce Pad", padColor, false);
        pad.transform.localScale = padSize;
        pad.transform.rotation = Quaternion.Euler(0f, 0f, side * padAngle);
        Vector3 extents = pad.GetComponent<Collider>().bounds.extents;
        pad.transform.position = new Vector3(
            camera.transform.position.x + side * Mathf.Max(0f, halfWidth - extents.x - 0.2f),
            Mathf.Max(Apple.bottomY + extents.y + 1f, camera.transform.position.y - camera.orthographicSize + extents.y + 0.2f), 0f);
    }

    private void UpdateShields()
    {
        if (shieldLeft == null || shieldRight == null || basketList.Count == 0) return;
        float bottom = -BasketBottomHeight * 0.5f;
        float top = basketSpacingY * (basketList.Count - 1) + BasketBottomHeight * 0.5f + shieldTopExtension;
        float height = top - bottom;
        float centerY = (top + bottom) * 0.5f;
        float depth = basketBottomPrefab.transform.localScale.z;
        LayoutShield(shieldLeft, -1f, height, centerY, depth);
        LayoutShield(shieldRight, 1f, height, centerY, depth);
    }

    private void LayoutShield(Transform shield, float side, float height, float centerY, float depth)
    {
        shield.localScale = new Vector3(shieldThickness, height, depth);
        shield.localPosition = new Vector3(side * (BasketHalfWidth + shieldThickness * 0.5f), centerY, 0f);
    }

    public void TreeDied()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        PauseGame();
        SaveRunScore();
        ShowEndGameModal("The tree died", "Give the tree time to recover between hits.\nScore: " + FormatScore());
    }

    private void ShowGameOverModal()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        PauseGame();
        SaveRunScore();
        ShowEndGameModal("Out of baskets", "Score: " + FormatScore());
    }

    private void SaveRunScore()
    {
        if (scoreCounter == null || !LeaderboardStore.HasPlayerName) return;
        LeaderboardStore.AddScore(LeaderboardStore.PlayerName, scoreCounter.score);
        HighScore.RefreshDisplay();
    }

    private void ShowShieldUnlockModal()
    {
        if (modal != null) Destroy(modal);
        Canvas canvas = FindAnyObjectByType<Canvas>();
        modal = new GameObject("Gameplay Modal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(canvas.transform, false);
        RectTransform overlay = modal.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.92f);

        GameObject buttonObject = new GameObject("Continue Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(modal.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(420f, 72f);
        buttonRect.anchoredPosition = Vector2.zero;
        buttonObject.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.55f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(ActivateShielding);
        TextMeshProUGUI label = AddModalText("You obtained SHIELDING", 0f, 28f);
        label.transform.SetParent(buttonObject.transform, false);
        label.rectTransform.sizeDelta = buttonRect.sizeDelta;
        button.Select();
    }

    private string FormatScore() => scoreCounter != null ? scoreCounter.score.ToString("#,0") : "0";

    private void PauseGame()
    {
        if (Time.timeScale > 0f) resumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
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

    private void ShowNameEntryModal()
    {
        if (modal != null) Destroy(modal);
        IsNamePromptOpen = true;
        PauseGame();

        Canvas canvas = FindAnyObjectByType<Canvas>();
        modal = new GameObject("Gameplay Modal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(canvas.transform, false);
        RectTransform overlay = modal.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.92f);

        AddModalText("Enter your name", 80f, 32f);
        TMP_InputField nameField = CreateNameInputField(modal.transform);
        nameField.transform.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, 10f);

        GameObject buttonObject = new GameObject("Start Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(modal.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(320f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, -90f);
        buttonObject.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.55f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(() => SubmitPlayerName(nameField));
        TextMeshProUGUI label = AddModalText("Start", 0f, 24f);
        label.transform.SetParent(buttonObject.transform, false);
        label.rectTransform.sizeDelta = buttonRect.sizeDelta;
        nameField.ActivateInputField();
        button.Select();
    }

    private void SubmitPlayerName(TMP_InputField nameField)
    {
        string name = nameField != null ? nameField.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(name)) return;
        LeaderboardStore.SetPlayerName(name);
        IsNamePromptOpen = false;
        if (modal != null) Destroy(modal);
        HighScore.RefreshDisplay();
        Time.timeScale = resumeTimeScale > 0f ? resumeTimeScale : 1f;
    }

    private TMP_InputField CreateNameInputField(Transform parent)
    {
        GameObject inputRoot = new GameObject("Name Input", typeof(RectTransform), typeof(Image));
        inputRoot.transform.SetParent(parent, false);
        RectTransform rootRect = inputRoot.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(360f, 48f);
        inputRoot.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.18f, 1f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputRoot.transform, false);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.offsetMin = new Vector2(10f, 6f);
        textAreaRect.offsetMax = new Vector2(-10f, -6f);

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI inputText = textObject.GetComponent<TextMeshProUGUI>();
        inputText.fontSize = 24f;
        inputText.color = Color.white;
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;

        GameObject placeholderObject = new GameObject("Placeholder", typeof(RectTransform), typeof(TextMeshProUGUI));
        placeholderObject.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholder = placeholderObject.GetComponent<TextMeshProUGUI>();
        placeholder.text = "Your name";
        placeholder.fontSize = 24f;
        placeholder.color = new Color(1f, 1f, 1f, 0.4f);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = placeholderRect.offsetMax = Vector2.zero;

        TMP_InputField field = inputRoot.AddComponent<TMP_InputField>();
        field.textViewport = textAreaRect;
        field.textComponent = inputText;
        field.placeholder = placeholder;
        return field;
    }

    private void ShowEndGameModal(string title, string description)
    {
        if (modal != null) Destroy(modal);
        Canvas canvas = FindAnyObjectByType<Canvas>();
        modal = new GameObject("Gameplay Modal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(canvas.transform, false);
        RectTransform overlay = modal.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        AddModalText(title, 100f, 34f);
        AddModalText(description, 20f, 19f);
        CreateModalButton("Restart", -80f, RestartGame);
        CreateModalButton("New Game", -160f, NewGame);
    }

    private void CreateModalButton(string label, float y, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(label + " Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(modal.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(320f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, y);
        buttonObject.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.55f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        TextMeshProUGUI buttonLabel = AddModalText(label, 0f, 22f);
        buttonLabel.transform.SetParent(buttonObject.transform, false);
        buttonLabel.rectTransform.sizeDelta = buttonRect.sizeDelta;
    }

    private void ShowModal(string title, string description, string buttonText, UnityEngine.Events.UnityAction action)
    {
        if (modal != null) Destroy(modal);
        Canvas canvas = FindAnyObjectByType<Canvas>();
        modal = new GameObject("Gameplay Modal", typeof(RectTransform), typeof(Image));
        modal.transform.SetParent(canvas.transform, false);
        RectTransform overlay = modal.GetComponent<RectTransform>();
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = overlay.offsetMax = Vector2.zero;
        modal.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.06f, 0.92f);
        AddModalText(title, 100f, 34f);
        AddModalText(description, 20f, 19f);

        GameObject buttonObject = new GameObject("Continue Button", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(modal.transform, false);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.sizeDelta = new Vector2(360f, 64f);
        buttonRect.anchoredPosition = new Vector2(0f, -90f);
        buttonObject.GetComponent<Image>().color = new Color(0.1f, 0.4f, 0.55f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.onClick.AddListener(action);
        TextMeshProUGUI label = AddModalText(buttonText, 0f, 22f);
        label.transform.SetParent(buttonObject.transform, false);
        label.rectTransform.sizeDelta = buttonRect.sizeDelta;
        button.Select();
    }

    private TextMeshProUGUI AddModalText(string text, float y, float fontSize)
    {
        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(modal.transform, false);
        TextMeshProUGUI label = textObject.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = Color.white;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.rectTransform.sizeDelta = new Vector2(580f, 90f);
        label.rectTransform.anchoredPosition = new Vector2(0f, y);
        return label;
    }

    void OnDestroy()
    {
        if (scoreCounter != null) scoreCounter.ScoreChanged -= OnScoreChanged;
        if (IsShieldPromptOpen || IsNamePromptOpen || IsGameOver) Time.timeScale = 1f;
        if (bounceMaterial != null) Destroy(bounceMaterial);
        if (padsRoot != null) Destroy(padsRoot);
        if (modal != null) Destroy(modal);
    }
}
