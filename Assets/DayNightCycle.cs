using UnityEngine;

public class DayNightCycle : MonoBehaviour
{
    [Min(1f)] public float cycleDurationInSeconds = 120f;

    // Directional lights shine along forward: downward means the sun is above the horizon.
    public bool IsNight => transform.forward.y >= 0f;

    void Update()
    {
        float rotationSpeed = 360f / Mathf.Max(1f, cycleDurationInSeconds);
        transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);
    }
}
