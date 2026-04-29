using UnityEngine;

public class PlayerAAnimationController : MonoBehaviour
{
    public Animator animator;
    public CharacterController characterController;

    public string speedParameterName = "Speed";
    public float speedThreshold = 0.1f;

    void Update()
    {
        if (animator == null || characterController == null) return;

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;

        float speed = horizontalVelocity.magnitude;

        animator.SetFloat(speedParameterName, speed);
    }
}
