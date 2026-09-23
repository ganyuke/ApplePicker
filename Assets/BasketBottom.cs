using UnityEngine;

// One stack tier. Trigger collider catches apples while the parent Basket rigidbody moves the rig.
public class BasketBottom : MonoBehaviour
{
    private ApplePicker picker;
    private ScoreCounter scoreCounter;

    void Awake()
    {
        picker = FindAnyObjectByType<ApplePicker>();
        scoreCounter = FindAnyObjectByType<ScoreCounter>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (picker == null || !picker.IsPlaying) return;
        Apple apple = other.GetComponent<Apple>();
        if (apple == null || !apple.TryConsume()) return;
        if (apple.type == AppleType.Poison) picker.AppleMissed();
        else if (scoreCounter != null)
            scoreCounter.AddPoints(apple.type == AppleType.Golden ? picker.goldenApplePoints : picker.normalApplePoints);
    }
}
