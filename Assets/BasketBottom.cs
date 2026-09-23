using UnityEngine;

// Catch logic for one stack tier. Basket routes collisions here (parent owns the rigidbody).
public class BasketBottom : MonoBehaviour
{
    private ApplePicker picker;
    private ScoreCounter scoreCounter;

    void Awake()
    {
        picker = FindAnyObjectByType<ApplePicker>();
        scoreCounter = FindAnyObjectByType<ScoreCounter>();
    }

    public void HandleAppleCollision(Collision collision)
    {
        if (picker == null || !picker.IsPlaying) return;
        Apple apple = collision.gameObject.GetComponent<Apple>();
        if (apple == null || !apple.TryConsume()) return;
        if (apple.type == AppleType.Poison) picker.AppleMissed();
        else if (scoreCounter != null)
            scoreCounter.AddPoints(apple.type == AppleType.Golden ? picker.goldenApplePoints : picker.normalApplePoints);
    }
}
