using System.Collections;
using UnityEngine;

/// <summary>
/// Melee enemy: attack animation, then dash through and past the player.
/// </summary>
public class EnemyMelee : EnemyBase
{
    [Header("Melee Dash")]
    public float attackRange = 7f;
    public float attackCooldown = 2.2f;
    public float dashSpeed = 14f;
    public float dashOvershoot = 2.5f;

    private float attackTimer;
    private bool isDashing;
    private Coroutine dashRoutine;

    protected override void Awake()
    {
        rotateVisualToTarget = false;
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        if (attackTimer > 0f)
            attackTimer -= Time.deltaTime;

        if (!isDashing && !IsPerformingAttack)
            TryBeginDashAttack();
    }

    protected override void FixedUpdate()
    {
        if (player == null || health.IsDead)
            return;

        if (isDashing)
            return;

        base.FixedUpdate();
    }

    private void TryBeginDashAttack()
    {
        if (attackTimer > 0f) return;
        if (DistanceToPlayer() > attackRange) return;

        Vector2 dir = DirectionToPlayer();
        float travelDistance = DistanceToPlayer() + dashOvershoot;

        attackTimer = attackCooldown;

        PlayAttackThen(() => StartDash(dir, travelDistance));
    }

    private void StartDash(Vector2 direction, float distance)
    {
        if (dashRoutine != null)
            StopCoroutine(dashRoutine);

        dashRoutine = StartCoroutine(DashRoutine(direction, distance));
    }

    private IEnumerator DashRoutine(Vector2 direction, float distance)
    {
        isDashing = true;
        float remaining = distance;

        while (remaining > 0f)
        {
            rb.linearVelocity = direction * dashSpeed;
            yield return new WaitForFixedUpdate();
            remaining -= dashSpeed * Time.fixedDeltaTime;
        }

        isDashing = false;
        rb.linearVelocity = Vector2.zero;
        dashRoutine = null;
    }
}
