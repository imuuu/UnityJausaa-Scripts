using UnityEngine;
using Animancer;

public class HandAnimator : MonoBehaviour
{
    public AnimancerComponent animancer;

    [SerializeField] private AnimationClip idleAnimation; // Idle animation
    [SerializeField] private AnimationClip[] grabAnimations; // Array of grab animations

    private void Update()
    {
        if (Input.GetMouseButtonDown(1)) // Right mouse button pressed
        {
            PlayRandomGrabAnimation();
        }
        else if (Input.GetMouseButtonUp(1)) // Right mouse button released
        {
            PlayIdleAnimation();
        }
    }

    private void PlayRandomGrabAnimation()
    {
        if (grabAnimations.Length > 0)
        {
            int randomIndex = Random.Range(0, grabAnimations.Length);
            animancer.Play(grabAnimations[randomIndex]);
        }
        else
        {
            Debug.LogWarning("No grab animations assigned!");
        }
    }

    private void PlayIdleAnimation()
    {
        if (idleAnimation != null)
        {
            animancer.Play(idleAnimation);
        }
        else
        {
            Debug.LogWarning("Idle animation not assigned!");
        }
    }
}
