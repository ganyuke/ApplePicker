using UnityEngine;

// Root basket rig: reads input, moves the stack, catches apples (RB must live here).
public class Basket : MonoBehaviour
{
    private ApplePicker picker;
    private ScoreCounter scoreCounter;
    private Rigidbody body;

    void Awake()
    {
        picker = FindAnyObjectByType<ApplePicker>();
        scoreCounter = FindAnyObjectByType<ScoreCounter>();
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

    void OnCollisionEnter(Collision collision)
    {
        if (picker == null || !picker.IsPlaying) return;
        Apple apple = collision.gameObject.GetComponent<Apple>();
        if (apple == null || !apple.TryConsume()) return;
        if (apple.type == AppleType.Poison) picker.AppleMissed();
        else if (scoreCounter != null)
            scoreCounter.AddPoints(apple.type == AppleType.Golden ? picker.goldenApplePoints : picker.normalApplePoints);
    }
}
