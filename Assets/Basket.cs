using UnityEngine;

// Root basket rig: reads input and moves the whole stack (bottoms + shields).
public class Basket : MonoBehaviour
{
    private ApplePicker picker;
    private Rigidbody body;

    void Awake()
    {
        picker = FindAnyObjectByType<ApplePicker>();
        body = GetComponent<Rigidbody>();
        if (body == null) return;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    void Update()
    {
        if (picker == null || !picker.IsPlaying || Camera.main == null) return;
        Vector3 mousePos2D = Input.mousePosition;
        mousePos2D.z = -Camera.main.transform.position.z;
        Vector3 mousePos3D = Camera.main.ScreenToWorldPoint(mousePos2D);
        picker.SetBasketTargetX(mousePos3D.x);
    }

    void FixedUpdate()
    {
        if (picker == null || !picker.IsPlaying || body == null) return;
        Vector3 pos = body.position;
        pos.x = picker.BasketX;
        body.MovePosition(pos);
    }

    // Shield colliders are children of this rigidbody; route hits to AppleBounceSurface on the collider that was hit.
    void OnCollisionEnter(Collision collision)
    {
        if (collision.contactCount == 0) return;
        AppleBounceSurface surface = collision.GetContact(0).thisCollider.GetComponent<AppleBounceSurface>();
        if (surface != null) surface.HandleAppleCollision(collision);
    }
}
