using UnityEngine;

/// <summary>
/// Ranged enemy: rotates at sprite pivot, attack animation, then fires at the player.
/// </summary>
public class EnemyRanged : EnemyBase
{
    [Header("References")]
    public GameObject projectilePrefab;

    [Header("Ranged Attack")]
    public float shootRange = 9f;
    public float stopRange = 5f;
    public float shootCooldown = 1.8f;
    public float projectileDamage = 3f;
    public float projectileSpeed = 15f;

    private float shootTimer;

    protected override void Awake()
    {
        rotateVisualToTarget = true;
        base.Awake();
    }

    protected override void Update()
    {
        base.Update();

        if (shootTimer > 0f)
            shootTimer -= Time.deltaTime;

        TryShoot();
    }

    protected override void UpdateMovement()
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;
        if (dist < 0.001f) return;

        Vector2 dir = toPlayer / dist;
        rb.linearVelocity = dist > stopRange ? dir * moveSpeed : Vector2.zero;
    }

    private void TryShoot()
    {
        if (IsPerformingAttack || shootTimer > 0f) return;
        if (projectilePrefab == null) return;
        if (DistanceToPlayer() > shootRange) return;

        Vector2 dir = DirectionToPlayer();
        shootTimer = shootCooldown;

        PlayAttackThen(() => SpawnProjectile(projectilePrefab, dir, projectileSpeed, projectileDamage));
    }
}
