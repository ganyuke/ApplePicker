using UnityEngine;

// One stack tier: collider + catch logic. Parent Basket rig handles movement.
public class BasketBottom : MonoBehaviour
{
    public ScoreCounter scoreCounter;
    private ApplePicker picker;

    void Start()
    {
        if (scoreCounter == null) scoreCounter = FindAnyObjectByType<ScoreCounter>();
        picker = FindAnyObjectByType<ApplePicker>();
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
