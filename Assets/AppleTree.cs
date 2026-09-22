using System.Collections.Generic;
using UnityEngine;

public class AppleTree : MonoBehaviour
{
    [Header("Inscribed")]
    
    public GameObject applePrefab;
    public float speed = 1f;
    public float leftAndRightEdge = 10f;
    public float changeDirChance = 0.1f;
    public float appleDropDelay = 1f;

    [Header("Day / Night")]
    public DayNightCycle dayNightCycle;
    [Range(0f, 1f)] public float nightSpeedMultiplier = 0.5f;
    [Range(0f, 1f)] public float dayPoisonChance = 0.05f;
    [Range(0f, 1f)] public float dayGoldenChance = 0.15f;
    [Range(0f, 1f)] public float nightPoisonChance = 0.25f;
    [Range(0f, 1f)] public float nightGoldenChance = 0.05f;

    [Header("Health / Stun")]
    [Min(1f)] public float maxHealth = 100f;
    [Min(0f)] public float hitDamage = 25f;
    [Min(0f)] public float stunDuration = 3f;
    [Min(0)] public int maxStunsPerWindow = 3;
    [Min(0.01f)] public float stunWindow = 15f;
    [Min(0f)] public float recoveryDelay = 5f;
    [Min(0f)] public float recoveryPerSecond = 5f;
    [Range(0f, 1f)] public float dyingBrightness = 0.15f;

    public float Health { get; private set; }
    public bool IsDead => Health <= 0f;
    public bool IsStunned => !IsDead && Time.time < stunnedUntil;
    public float CurrentSpeed => IsDead || IsStunned ? 0f :
        speed * (dayNightCycle != null && dayNightCycle.IsNight ? nightSpeedMultiplier : 1f);

    private ApplePicker picker;
    private Rigidbody body;
    private float stunnedUntil;
    private float lastHitTime = float.NegativeInfinity;
    private readonly Queue<float> stunTimes = new Queue<float>();
    private Renderer[] treeRenderers;
    private Color[] healthyColors;
    private MaterialPropertyBlock colorProperties;

    void Awake()
    {
        Health = maxHealth;
        body = GetComponent<Rigidbody>();
        if (body != null)
        {
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        }
        treeRenderers = GetComponentsInChildren<Renderer>();
        healthyColors = new Color[treeRenderers.Length];
        colorProperties = new MaterialPropertyBlock();
        for (int i = 0; i < treeRenderers.Length; i++)
        {
            Material material = treeRenderers[i].sharedMaterial;
            healthyColors[i] = material != null ? material.color : Color.white;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        picker = FindAnyObjectByType<ApplePicker>();
        if (dayNightCycle == null) dayNightCycle = FindAnyObjectByType<DayNightCycle>();
        Invoke(nameof(DropApple), 2f);
    }

    void DropApple()
    {
        if (IsDead) return;
        if (picker == null || picker.IsPlaying)
        {
            // Keep spawning INSIDE the tree. The Apple layer ignores the tree until returned.
            GameObject apple = Instantiate(applePrefab, transform.position, Quaternion.identity);
            apple.GetComponent<Apple>().Initialize(ChooseAppleType(Random.value));
        }
        Invoke(nameof(DropApple), Mathf.Max(0.01f, appleDropDelay));
    }

    public AppleType ChooseAppleType(float roll)
    {
        if (IsStunned) return AppleType.Normal;
        bool night = dayNightCycle != null && dayNightCycle.IsNight;
        float poison = Mathf.Clamp01(night ? nightPoisonChance : dayPoisonChance);
        float golden = Mathf.Clamp(night ? nightGoldenChance : dayGoldenChance, 0f, 1f - poison);
        if (roll < poison) return AppleType.Poison;
        if (roll < poison + golden) return AppleType.Golden;
        return AppleType.Normal;
    }

    // Update is called once per frame
    void Update()
    {
        if (IsDead || (picker != null && !picker.IsPlaying)) return;
        // Account only for the portion of this frame after the recovery delay has elapsed.
        float recoveryTime = Mathf.Min(Time.deltaTime, Mathf.Max(0f, Time.time - lastHitTime - recoveryDelay));
        if (Health < maxHealth && recoveryTime > 0f)
        {
            Health = Mathf.Min(maxHealth, Health + recoveryPerSecond * recoveryTime);
            UpdateHealthColors();
        }
    }

    void FixedUpdate()
    {
        if (IsDead || IsStunned || (picker != null && !picker.IsPlaying)) return;
        Vector3 pos = body != null ? body.position : transform.position;
        pos.x += CurrentSpeed * Time.fixedDeltaTime;
        if (pos.x < -leftAndRightEdge)
        {
            pos.x = -leftAndRightEdge;
            speed = Mathf.Abs(speed);
        }
        else if (pos.x > leftAndRightEdge)
        {
            pos.x = leftAndRightEdge;
            speed = -Mathf.Abs(speed);
        }
        if (body != null) body.MovePosition(pos);
        else transform.position = pos;

        if (Random.value < changeDirChance)
        {
            speed *= -1;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Apple apple = collision.gameObject.GetComponent<Apple>();
        if (apple != null) TryHit(apple);
    }

    public bool TryHit(Apple apple)
    {
        if (IsDead || (picker != null && !picker.IsPlaying) || apple == null ||
            !apple.IsReturned || !apple.TryConsume()) return false;

        lastHitTime = Time.time;
        Health = Mathf.Max(0f, Health - hitDamage);
        UpdateHealthColors();
        if (IsDead)
        {
            CancelInvoke(nameof(DropApple));
            if (picker != null) picker.TreeDied();
            return true;
        }

        while (stunTimes.Count > 0 && Time.time - stunTimes.Peek() >= stunWindow)
            stunTimes.Dequeue();
        if (stunTimes.Count < maxStunsPerWindow)
        {
            stunTimes.Enqueue(Time.time);
            stunnedUntil = Time.time + stunDuration;
        }
        return true;
    }

    private void UpdateHealthColors()
    {
        float brightness = Mathf.Lerp(dyingBrightness, 1f, Health / Mathf.Max(1f, maxHealth));
        for (int i = 0; i < treeRenderers.Length; i++)
        {
            Color color = healthyColors[i] * brightness;
            color.a = healthyColors[i].a;
            treeRenderers[i].GetPropertyBlock(colorProperties);
            colorProperties.SetColor("_BaseColor", color);
            colorProperties.SetColor("_Color", color);
            treeRenderers[i].SetPropertyBlock(colorProperties);
        }
    }
}
