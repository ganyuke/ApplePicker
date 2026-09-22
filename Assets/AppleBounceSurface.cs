using UnityEngine;

// Shared by the kinematic side shields and static corner pads.
// The physics material handles reflection, including the moving shield's velocity.
public class AppleBounceSurface : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        Apple apple = collision.gameObject.GetComponent<Apple>();
        if (apple != null) apple.MarkReturned();
    }
}
