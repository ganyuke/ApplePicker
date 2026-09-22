using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[DefaultExecutionOrder(-100)]
public class ApplePicker : MonoBehaviour
{
    [Header("Inscribed")]
    public GameObject basketPrefab;
    public int numBaskets = 3;
    public float basketBottomY = -14f;
    public float basketSpacingY = 2f;
    public List<GameObject> basketList;

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
    public Color shieldColor = new Color(0.2f, 0.85f, 1f);
    public Color padColor = new Color(1f, 0.55f, 0.15f);

    public bool ShieldingUnlocked { get; private set; }
    public bool IsShieldPromptOpen { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsPlaying => !IsShieldPromptOpen && !IsGameOver && Time.timeScale > 0f;
    public float BasketX { get; private set; }
    public long NextBasketRewardScore { get; private set; }

    private ScoreCounter scoreCounter;
    private float targetBasketX;
    private int lastLossFrame = -1;
    private Rigidbody leftShield;
    private Rigidbody rightShield;
    private GameObject shieldingRoot;
    private GameObject modal;
    private PhysicsMaterial bounceMaterial;
    private float resumeTimeScale = 1f;

    void Start()
    {
        basketList = new List<GameObject>();
        NextBasketRewardScore = Mathf.Max(1, firstBasketRewardScore);
        for (int i = 0; i < numBaskets; i++) RestoreBasket();
        scoreCounter = FindAnyObjectByType<ScoreCounter>();
        if (scoreCounter != null)
        {
            scoreCounter.ScoreChanged += OnScoreChanged;
            OnScoreChanged(scoreCounter.score);
        }
    }

    void FixedUpdate()
    {
        if (!IsPlaying) return;
        BasketX = Mathf.MoveTowards(BasketX, ClampBasketX(targetBasketX), basketMoveSpeed * Time.fixedDeltaTime);
        if (ShieldingUnlocked) UpdateShields(false);
    }

    public void SetBasketTargetX(float x)
    {
        if (IsPlaying) targetBasketX = ClampBasketX(x);
    }

    private float BasketHalfWidth => basketPrefab.transform.localScale.x *
        basketPrefab.GetComponent<BoxCollider>().size.x * 0.5f;

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
            ShowModal("Shielding unlocked", "Use the side shields and corner pads to return apples.\nToo many hits will kill the tree.",
                "You obtained SHIELDING", ActivateShielding);
        }
    }

    public bool RestoreBasket()
    {
        if (basketList.Count >= numBaskets) return false;
        Vector3 pos = new Vector3(BasketX, basketBottomY + basketSpacingY * basketList.Count, 0f);
        GameObject basket = Instantiate(basketPrefab, pos, basketPrefab.transform.rotation);
        basketList.Add(basket);
        if (ShieldingUnlocked) UpdateShields(true);
        return true;
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
        GameObject basketGO = basketList[basketIndex];
        basketList.RemoveAt(basketIndex);
        basketGO.SetActive(false);
        Destroy(basketGO);
        if (basketList.Count == 0)
        {
            IsGameOver = true;
            RestartGame();
        }
        else if (ShieldingUnlocked) UpdateShields(true);
    }

    public void ActivateShielding()
    {
        if (!IsShieldPromptOpen || IsGameOver || ShieldingUnlocked) return;
        ShieldingUnlocked = true;
        shieldingRoot = new GameObject("Shielding");
        bounceMaterial = new PhysicsMaterial("Shield and pad bounce")
        {
            bounciness = 1f,
            dynamicFriction = 0f,
            staticFriction = 0f,
            bounceCombine = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum
        };
        leftShield = CreateBounceSurface("Left Shield", shieldColor, true).GetComponent<Rigidbody>();
        rightShield = CreateBounceSurface("Right Shield", shieldColor, true).GetComponent<Rigidbody>();
        BasketX = ClampBasketX(BasketX);
        targetBasketX = BasketX;
        foreach (GameObject basket in basketList)
        {
            Rigidbody body = basket.GetComponent<Rigidbody>();
            body.position = new Vector3(BasketX, body.position.y, body.position.z);
        }
        UpdateShields(true);
        CreatePad(-1f);
        CreatePad(1f);
        IsShieldPromptOpen = false;
        if (modal != null) Destroy(modal);
        Time.timeScale = resumeTimeScale;
    }

    private GameObject CreateBounceSurface(string surfaceName, Color color, bool moving)
    {
        GameObject surface = GameObject.CreatePrimitive(PrimitiveType.Cube);
        surface.name = surfaceName;
        surface.transform.SetParent(shieldingRoot.transform);
        surface.layer = LayerMask.NameToLayer("Basket");
        surface.GetComponent<Collider>().sharedMaterial = bounceMaterial;
        Renderer surfaceRenderer = surface.GetComponent<Renderer>();
        surfaceRenderer.sharedMaterial = basketPrefab.GetComponent<Renderer>().sharedMaterial;
        var properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", color);
        properties.SetColor("_Color", color);
        surfaceRenderer.SetPropertyBlock(properties);
        surface.AddComponent<AppleBounceSurface>();
        if (moving)
        {
            Rigidbody body = surface.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
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

    private void UpdateShields(bool teleport)
    {
        if (leftShield == null || rightShield == null || basketList.Count == 0) return;
        float basketHeight = basketPrefab.transform.localScale.y * basketPrefab.GetComponent<BoxCollider>().size.y;
        float bottom = basketBottomY - basketHeight * 0.5f;
        float top = basketBottomY + basketSpacingY * (basketList.Count - 1) + basketHeight * 0.5f + shieldTopExtension;
        Vector3 size = new Vector3(shieldThickness, top - bottom, basketPrefab.transform.localScale.z);
        MoveShield(leftShield, -1f, size, (top + bottom) * 0.5f, teleport);
        MoveShield(rightShield, 1f, size, (top + bottom) * 0.5f, teleport);
    }

    private void MoveShield(Rigidbody body, float side, Vector3 size, float y, bool teleport)
    {
        if (body.transform.localScale != size) body.transform.localScale = size;
        Vector3 position = new Vector3(BasketX + side * (BasketHalfWidth + shieldThickness * 0.5f), y, 0f);
        if (teleport) body.position = position;
        else body.MovePosition(position);
    }

    public void TreeDied()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        PauseGame();
        ShowModal("The tree died", "Give the tree time to recover between hits.\nScore: " +
            (scoreCounter != null ? scoreCounter.score.ToString("#,0") : "0"), "Restart", RestartGame);
    }

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
        if (IsShieldPromptOpen || IsGameOver) Time.timeScale = 1f;
        if (bounceMaterial != null) Destroy(bounceMaterial);
        if (shieldingRoot != null) Destroy(shieldingRoot);
        if (modal != null) Destroy(modal);
    }
}
