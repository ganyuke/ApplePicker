using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class ApplePicker : MonoBehaviour
{
    [Header("Baskets")]
    [Tooltip("Required. Drag Assets/BasketBottom.prefab from the Project window.")]
    public GameObject basketBottomPrefab;
    public int numBaskets = 3;
    public float basketBottomY = -14f;
    public float basketSpacingY = 2f;

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

    [Header("Dev")]
    public bool devPadsAlwaysAimAtTree;

    public bool DevPadsAlwaysAimAtTree => devPadsAlwaysAimAtTree;
    public bool ShieldingUnlocked { get; private set; }
    public bool IsGameOver { get; private set; }
    public bool IsShieldPromptOpen => gameUI != null && gameUI.IsShieldPromptOpen;
    public bool IsNamePromptOpen => gameUI != null && gameUI.IsNamePromptOpen;
    public bool IsPlaying => !IsGameOver && (gameUI == null || !gameUI.BlocksGameplay) && Time.timeScale > 0f;
    public float BasketX { get; private set; }
    public long NextBasketRewardScore { get; private set; }

    private Basket basketRig;
    private Transform basketBottomsParent;
    private GameObject shieldsRoot;
    private Transform shieldLeft;
    private Transform shieldRight;
    private readonly List<GameObject> basketList = new List<GameObject>();

    private GameUIManager gameUI;
    private ScoreCounter scoreCounter;
    private float targetBasketX;
    private int lastLossFrame = -1;
    private PhysicsMaterial bounceMaterial;
    private GameObject padsRoot;
    private BoxCollider basketBottomCollider;

    void Awake()
    {
        ResolveReferences();
    }

    void OnValidate()
    {
        if (basketBottomPrefab != null && !basketBottomPrefab.scene.IsValid())
            basketBottomCollider = basketBottomPrefab.GetComponent<BoxCollider>();
    }

    void Start()
    {
        ResolveReferences();
        PositionBasketRig();

        gameUI = FindAnyObjectByType<GameUIManager>();
        if (gameUI != null)
        {
            gameUI.ShieldAccepted += ActivateShielding;
        }

        if (shieldsRoot != null) shieldsRoot.SetActive(false);

        NextBasketRewardScore = Mathf.Max(1, firstBasketRewardScore);
        if (!ValidateBasketBottomPrefab()) return;
        ClearBasketBottoms();
        for (int i = 0; i < numBaskets; i++) RestoreBasket();
        scoreCounter = FindAnyObjectByType<ScoreCounter>();
        if (scoreCounter != null)
        {
            scoreCounter.ScoreChanged += OnScoreChanged;
            OnScoreChanged(scoreCounter.score);
        }
    }

    private void ResolveReferences()
    {
        basketRig = FindAnyObjectByType<Basket>();
        if (basketRig == null)
        {
            Debug.LogWarning("ApplePicker: no Basket found in scene.", this);
            return;
        }

        basketBottomsParent = FindChildTransform(basketRig.transform, "BasketBottoms");
        if (basketBottomsParent == null)
            basketBottomsParent = basketRig.transform;

        Transform shields = FindChildTransform(basketRig.transform, "Shields");
        shieldsRoot = shields != null ? shields.gameObject : null;
        if (shields != null)
        {
            shieldLeft = FindChildTransform(shields, "ShieldLeft");
            shieldRight = FindChildTransform(shields, "ShieldRight", "Right");
        }

        basketBottomCollider = basketBottomPrefab.GetComponent<BoxCollider>();
    }

    private bool ValidateBasketBottomPrefab()
    {
        if (basketBottomPrefab == null)
        {
            Debug.LogError("ApplePicker: Basket Bottom Prefab is required. Assign Assets/BasketBottom.prefab.", this);
            return false;
        }

        if (basketBottomPrefab.scene.IsValid())
        {
            Debug.LogError("ApplePicker: Basket Bottom Prefab must be Assets/BasketBottom.prefab, not a scene object.", this);
            return false;
        }

        if (basketBottomPrefab.GetComponent<BoxCollider>() == null)
        {
            Debug.LogError("ApplePicker: Basket Bottom Prefab must have a BoxCollider.", this);
            return false;
        }

        basketBottomCollider = basketBottomPrefab.GetComponent<BoxCollider>();
        return true;
    }

    private static Transform FindChildTransform(Transform parent, string exactName, string nameContains = null)
    {
        Transform direct = parent.Find(exactName);
        if (direct != null) return direct;
        if (string.IsNullOrEmpty(nameContains)) return null;
        foreach (Transform child in parent)
        {
            if (child.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
                return child;
        }
        return null;
    }

    void FixedUpdate()
    {
        if (!IsPlaying) return;
        BasketX = Mathf.MoveTowards(BasketX, ClampBasketX(targetBasketX), basketMoveSpeed * Time.fixedDeltaTime);
    }

    public void SetBasketTargetX(float x)
    {
        if (IsPlaying) targetBasketX = ClampBasketX(x);
    }

    private float BasketHalfWidth
    {
        get
        {
            if (basketBottomPrefab == null || basketBottomCollider == null) return 2f;
            return basketBottomPrefab.transform.localScale.x * basketBottomCollider.size.x * 0.5f;
        }
    }

    private float BasketBottomHeight
    {
        get
        {
            if (basketBottomPrefab == null || basketBottomCollider == null) return 1f;
            return basketBottomPrefab.transform.localScale.y * basketBottomCollider.size.y;
        }
    }

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
        if (score >= shieldingScore && !ShieldingUnlocked && !IsShieldPromptOpen && gameUI != null)
            gameUI.ShowShieldUnlock();
    }

    private void ClearBasketBottoms()
    {
        basketList.Clear();
        if (basketBottomsParent == null) return;
        for (int i = basketBottomsParent.childCount - 1; i >= 0; i--)
            Destroy(basketBottomsParent.GetChild(i).gameObject);
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
            EndRun("Out of baskets", "Score: " + FormatScore());
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
        gameUI?.HideShieldUnlock();
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

    private static void EnsureKinematicRigidbody(GameObject target)
    {
        Rigidbody body = target.GetComponent<Rigidbody>();
        if (body == null) body = target.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
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
        EnsureKinematicRigidbody(surface);
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
        float depth = basketBottomPrefab != null ? basketBottomPrefab.transform.localScale.z : 4f;
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
        EndRun("The tree died", "Give the tree time to recover between hits.\nScore: " + FormatScore());
    }

    private void EndRun(string title, string description)
    {
        if (IsGameOver) return;
        IsGameOver = true;
        SaveRunScore();
        gameUI?.ShowGameOver(title, description);
    }

    private void SaveRunScore()
    {
        if (scoreCounter == null || !LeaderboardStore.HasPlayerName) return;
        LeaderboardStore.AddScore(LeaderboardStore.PlayerName, scoreCounter.score);
        Leaderboard.RefreshDisplay();
    }

    private string FormatScore() => scoreCounter != null ? scoreCounter.score.ToString("#,0") : "0";

    public void RestartGame() => gameUI?.RestartGame();

    public void NewGame() => gameUI?.NewGame();

    void OnDestroy()
    {
        if (scoreCounter != null) scoreCounter.ScoreChanged -= OnScoreChanged;
        if (gameUI != null)
        {
            gameUI.ShieldAccepted -= ActivateShielding;
        }
        if (IsGameOver || IsShieldPromptOpen || IsNamePromptOpen) Time.timeScale = 1f;
        if (bounceMaterial != null) Destroy(bounceMaterial);
        if (padsRoot != null) Destroy(padsRoot);
    }
}
