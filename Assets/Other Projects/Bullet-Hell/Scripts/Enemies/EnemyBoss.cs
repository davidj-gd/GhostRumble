using UnityEngine;

/// <summary>
/// Boss: attack animation, then a big center shot plus homing shots on each fan edge.
/// </summary>
public class EnemyBoss : EnemyBase
{
    [Header("References")]
    public GameObject projectilePrefab;

    [Header("Boss Attack")]
    public float shootRange = 13f;
    public float stopRange = 7f;
    public float shootCooldown = 3f;
    public float centerDamage = 12f;
    public float homingDamage = 6f;
    public float centerSpeed = 9f;
    public float homingSpeed = 11f;
    public float centerScale = 1.8f;
    public float fanSpreadDegrees = 42f;

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

        TryBossAttack();
    }

    protected override void UpdateMovement()
    {
        Vector2 toPlayer = (Vector2)player.position - rb.position;
        float dist = toPlayer.magnitude;
        if (dist < 0.001f) return;

        Vector2 dir = toPlayer / dist;
        rb.linearVelocity = dist > stopRange ? dir * (moveSpeed * 0.75f) : Vector2.zero;
    }

    private void TryBossAttack()
    {
        if (IsPerformingAttack || shootTimer > 0f) return;
        if (projectilePrefab == null) return;
        if (DistanceToPlayer() > shootRange) return;

        Vector2 baseDir = DirectionToPlayer();
        float baseAngle = Mathf.Atan2(baseDir.y, baseDir.x) * Mathf.Rad2Deg;
        float halfSpread = fanSpreadDegrees * 0.5f;
        shootTimer = shootCooldown;

        PlayAttackThen(() =>
        {
            SpawnProjectile(projectilePrefab, baseDir, centerSpeed, centerDamage, centerScale, homing: false);

            Vector2 leftDir = AngleToDirection(baseAngle - halfSpread);
            Vector2 rightDir = AngleToDirection(baseAngle + halfSpread);
            SpawnProjectile(projectilePrefab, leftDir, homingSpeed, homingDamage, 1f, homing: true);
            SpawnProjectile(projectilePrefab, rightDir, homingSpeed, homingDamage, 1f, homing: true);
        });
    }

    private static Vector2 AngleToDirection(float angleDegrees)
    {
        float rad = angleDegrees * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
    }
}
