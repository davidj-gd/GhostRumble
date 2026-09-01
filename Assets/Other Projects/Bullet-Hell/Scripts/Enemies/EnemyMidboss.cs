using UnityEngine;

/// <summary>
/// Mid-boss: attack animation, then 5 fast projectiles in a fan toward the player.
/// </summary>
public class EnemyMidboss : EnemyBase
{
    [Header("References")]
    public GameObject projectilePrefab;

    [Header("Fan Attack")]
    public float shootRange = 11f;
    public float stopRange = 6f;
    public float shootCooldown = 2.5f;
    public float projectileDamage = 5f;
    public float projectileSpeed = 16f;
    public int fanCount = 5;
    public float fanSpreadDegrees = 50f;

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

        TryFanAttack();
    }

    protected override void UpdateMovement()
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;
        if (dist < 0.001f) return;

        Vector2 dir = toPlayer / dist;
        rb.linearVelocity = dist > stopRange ? dir * moveSpeed : Vector2.zero;
    }

    private void TryFanAttack()
    {
        if (IsPerformingAttack || shootTimer > 0f) return;
        if (projectilePrefab == null) return;
        if (DistanceToPlayer() > shootRange) return;

        Vector2 baseDir = DirectionToPlayer();
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        shootTimer = shootCooldown;

        PlayAttackThen(() =>
        {
            int count = Mathf.Max(1, fanCount);
            float spread = fanSpreadDegrees;
            float startAngle = baseAngle - spread * 0.5f;
            float step = count > 1 ? spread / (count - 1) : 0f;

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i;
                Vector2 dir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
                SpawnProjectile(projectilePrefab, dir, projectileSpeed, projectileDamage);
            }

        });
    }
}
