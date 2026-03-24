using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPunch : MonoBehaviour
{
    [Header("Punch References")]
    [SerializeField] private PunchProjectile punchProjectile;

    [SerializeField] Animator animator;

    [Header("Cooldown")]
    [SerializeField] private float punchCooldown = 0.4f;

    float nextPunchAllowedTime = -1f;

    public float PunchCooldownDuration => punchCooldown;

    public float PunchCooldownRemaining => Mathf.Max(0f, nextPunchAllowedTime - Time.time);

    public float PunchCooldownNormalized01 => punchCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(PunchCooldownRemaining / punchCooldown);

    public bool CanPunch => Time.time >= nextPunchAllowedTime;

    private void Awake()
    {
        if (punchProjectile == null)
        {
            punchProjectile = GetComponent<PunchProjectile>();
        } 
    }

    public void OnPunch(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;

        if (!CanPunch)
            return;

        animator.SetTrigger("Punch");
        punchProjectile.ThrowPunch();
        nextPunchAllowedTime = Time.time + punchCooldown;
    }
}
