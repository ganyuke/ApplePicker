using UnityEngine;

public enum AppleType { Normal, Poison, Golden }

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class Apple : MonoBehaviour
{
    public static float bottomY = -20f;

    public AppleType type;
    public Color poisonColor = new Color(0.65f, 0.12f, 0.9f);
    public Color goldenColor = new Color(1f, 0.72f, 0.04f);
    public bool IsReturned { get; private set; }
    public bool IsResolved { get; private set; }

    private Rigidbody body;
    private ApplePicker picker;

    void Awake()
    {
        body = GetComponent<Rigidbody>();
        body.constraints |= RigidbodyConstraints.FreezePositionZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        picker = FindAnyObjectByType<ApplePicker>();
        gameObject.layer = LayerMask.NameToLayer("Apple");
    }

    public void Initialize(AppleType appleType)
    {
        type = appleType;
        Renderer appleRenderer = GetComponent<Renderer>();
        if (appleRenderer == null) return;
        var properties = new MaterialPropertyBlock();
        if (type != AppleType.Normal)
        {
            Color color = type == AppleType.Poison ? poisonColor : goldenColor;
            properties.SetColor("_BaseColor", color);
            properties.SetColor("_Color", color);
        }
        appleRenderer.SetPropertyBlock(properties);
    }

    public void MarkReturned()
    {
        if (IsResolved || IsReturned) return;
        IsReturned = true;
        gameObject.layer = LayerMask.NameToLayer("ReturnedApple");
    }

    // Resolve immediately; Destroy alone is deferred and allows duplicate collision callbacks.
    public bool TryConsume()
    {
        if (IsResolved) return false;
        IsResolved = true;
        body.detectCollisions = false;
        foreach (Collider appleCollider in GetComponentsInChildren<Collider>())
            appleCollider.enabled = false;
        Destroy(gameObject);
        return true;
    }

    void Update()
    {
        if (IsResolved || (picker != null && !picker.IsPlaying)) return;
        if (transform.position.y < bottomY && TryConsume())
        {
            if (type == AppleType.Normal && picker != null) picker.AppleMissed();
        }
    }
}
