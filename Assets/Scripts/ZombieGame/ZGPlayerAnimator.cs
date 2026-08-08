using UnityEngine;

public sealed class ZGPlayerAnimator : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ZGWeaponController weapon;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
        if (weapon == null)
        {
            weapon = GetComponent<ZGWeaponController>();
        }
    }

    private void OnEnable()
    {
        if (weapon != null)
        {
            weapon.Fired += OnFired;
            weapon.ReloadStarted += OnReloadStarted;
        }
    }

    private void OnDisable()
    {
        if (weapon != null)
        {
            weapon.Fired -= OnFired;
            weapon.ReloadStarted -= OnReloadStarted;
        }
    }

    private void Update()
    {
        if (animator == null || controller == null)
        {
            return;
        }

        var flatVelocity = controller.velocity;
        flatVelocity.y = 0f;
        animator.SetFloat("Speed", flatVelocity.magnitude, 0.12f, Time.deltaTime);
        animator.SetBool("Grounded", controller.isGrounded);
    }

    private void OnFired()
    {
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
    }

    private void OnReloadStarted()
    {
        if (animator != null)
        {
            animator.SetTrigger("Reload");
        }
    }
}
