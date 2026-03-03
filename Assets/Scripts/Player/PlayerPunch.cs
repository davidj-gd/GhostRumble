using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerPunch : MonoBehaviour
{
    [Header("Punch References")]
    [SerializeField] private PunchProjectile punchProjectile;

    [SerializeField] Animator animator;

    private void Awake()
    {
        if (punchProjectile == null)
        {
            punchProjectile = GetComponent<PunchProjectile>();
        } 
    }

    public void OnPunch(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            animator.SetTrigger("Punch");
            punchProjectile.ThrowPunch();
        }
        else
        {
            return;
        }
    }
}
