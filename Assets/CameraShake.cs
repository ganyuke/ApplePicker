using UnityEngine;

[DisallowMultipleComponent]
public class CameraShake : MonoBehaviour
{
    [SerializeField] float defaultDuration = 0.15f;
    [SerializeField] float defaultMagnitude = 0.12f;

    private Vector3 anchorPosition;
    private float shakeUntil;
    private float shakeDuration;
    private float shakeMagnitude;

    void Awake()
    {
        anchorPosition = transform.localPosition;
    }

    void LateUpdate()
    {
        if (Time.unscaledTime >= shakeUntil)
        {
            transform.localPosition = anchorPosition;
            return;
        }

        float duration = Mathf.Max(0.01f, shakeDuration);
        float falloff = 1f - (shakeUntil - Time.unscaledTime) / duration;
        float magnitude = shakeMagnitude * Mathf.Clamp01(falloff);
        transform.localPosition = anchorPosition + (Vector3)Random.insideUnitCircle * magnitude;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeUntil = Time.unscaledTime + duration;
        shakeMagnitude = magnitude;
    }

    public static void ShakeCamera(float duration, float magnitude)
    {
        CameraShake shake = Camera.main != null ? Camera.main.GetComponent<CameraShake>() : null;
        if (shake == null) shake = FindAnyObjectByType<CameraShake>();
        if (shake != null) shake.Shake(duration, magnitude);
    }
}
