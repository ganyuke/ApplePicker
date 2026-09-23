using UnityEngine;

// Shared by the kinematic side shields and static corner pads.
public class AppleBounceSurface : MonoBehaviour
{
    public bool isShield;
    [Range(0f, 1f)] public float padAimStrength = 0.65f;
    public ParticleSystem shieldHitEffectPrefab;

    void OnCollisionEnter(Collision collision) => HandleAppleCollision(collision);

    public void HandleAppleCollision(Collision collision)
    {
        Apple apple = collision.gameObject.GetComponent<Apple>();
        if (apple == null) return;

        if (isShield)
        {
            apple.MarkShieldReturned();
            PlayShieldHitEffect(collision.GetContact(0).point);
            return;
        }

        apple.MarkReturned();
        AimTowardTree(apple);
    }

    private void AimTowardTree(Apple apple)
    {
        if (padAimStrength <= 0f) return;
        AppleTree tree = FindAnyObjectByType<AppleTree>();
        Rigidbody body = apple.GetComponent<Rigidbody>();
        if (tree == null || body == null) return;

        Vector3 velocity = body.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 0.1f) return;

        Vector3 toTree = tree.transform.position - apple.transform.position;
        toTree.z = 0f;
        if (toTree.sqrMagnitude < 0.01f) return;

        Vector3 aimed = Vector3.Lerp(velocity.normalized, toTree.normalized, padAimStrength).normalized * speed;
        aimed.z = 0f;
        body.linearVelocity = aimed;
    }

    private void PlayShieldHitEffect(Vector3 position)
    {
        if (shieldHitEffectPrefab == null) return;
        ParticleSystem effect = Instantiate(shieldHitEffectPrefab, position, Quaternion.identity);
        effect.Play();
        Destroy(effect.gameObject, effect.main.duration + effect.main.startLifetime.constantMax);
    }
}
