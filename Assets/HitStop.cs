using UnityEngine;

[DisallowMultipleComponent]
public class HitStop : MonoBehaviour
{
    [SerializeField] float defaultDuration = 0.07f;

    private float resumeAt = -1f;
    private float resumeTimeScale = 1f;

    public void Freeze(float duration)
    {
        if (Time.timeScale <= 0f) return;
        resumeTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        resumeAt = Time.unscaledTime + duration;
    }

    void Update()
    {
        if (resumeAt < 0f) return;
        if (Time.unscaledTime < resumeAt) return;
        Time.timeScale = resumeTimeScale > 0f ? resumeTimeScale : 1f;
        resumeAt = -1f;
    }

    public static void FreezeTime(float duration)
    {
        HitStop hitStop = FindAnyObjectByType<HitStop>();
        if (hitStop != null) hitStop.Freeze(duration);
    }
}
