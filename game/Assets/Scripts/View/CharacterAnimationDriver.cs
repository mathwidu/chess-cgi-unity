using UnityEngine;

public sealed class CharacterAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public bool HasAnimator => animator != null;

    public void Configure(Animator targetAnimator)
    {
        animator = targetAnimator;
    }

    private void Awake()
    {
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    public bool TryPlay(string stateName)
    {
        if (animator == null || string.IsNullOrEmpty(stateName))
        {
            return false;
        }

        animator.Play(stateName);
        return true;
    }
}
