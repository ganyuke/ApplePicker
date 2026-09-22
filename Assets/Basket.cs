using UnityEngine;

public class Basket : MonoBehaviour
{
    public ScoreCounter scoreCounter;
    private ApplePicker picker;
    private Rigidbody body;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (scoreCounter == null) scoreCounter = FindAnyObjectByType<ScoreCounter>();
        picker = FindAnyObjectByType<ApplePicker>();
        body = GetComponent<Rigidbody>();
        body.interpolation = RigidbodyInterpolation.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    // Update is called once per frame
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
        if (picker == null || !picker.IsPlaying) return;
        Vector3 pos = body.position;
        pos.x = picker.BasketX;
        body.MovePosition(pos);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (picker == null || !picker.IsPlaying) return;
        Apple apple = collision.gameObject.GetComponent<Apple>();
        if (apple != null && apple.TryConsume())
        {
            if (apple.type == AppleType.Poison) picker.AppleMissed();
            else if (scoreCounter != null)
                scoreCounter.AddPoints(apple.type == AppleType.Golden ? picker.goldenApplePoints : picker.normalApplePoints);
        }
    }
}
