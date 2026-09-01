using UnityEngine;

/// <summary>
/// Central place for the player to receive damage (bullets + enemy body contact).
/// Adds brief invulnerability so overlapping hits do not stack every frame.
///
/// Setup: add to the Player alongside Health.
/// </summary>
[RequireComponent(typeof(Health))]
public class PlayerHurtHandler : MonoBehaviour
{
    [Header("Invulnerability")]
    public float defaultInvulnDuration = 0.25f;

    private Health health;
    private float invulnTimer;

    public bool CanTakeDamage => !health.IsDead && invulnTimer <= 0f;

    private void Awake()
    {
        health = GetComponent<Health>();
        EnsureHurtboxTrigger();
    }

    private void Update()
    {
        if (invulnTimer > 0f)
            invulnTimer -= Time.deltaTime;
    }

    public void ReceiveDamage(float amount, float invulnDuration = -1f)
    {
        if (!CanTakeDamage || amount <= 0f) return;

        health.TakeDamage(amount);
        invulnTimer = invulnDuration >= 0f ? invulnDuration : defaultInvulnDuration;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryBulletHit(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryBulletHit(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryEnemyContact(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryEnemyContact(collision.collider);
    }

    private void TryBulletHit(Collider2D other)
    {
        Projectile projectile = other.GetComponent<Projectile>();
        if (projectile == null || !projectile.CanHitPlayer) return;

        ReceiveDamage(projectile.DamageAmount, defaultInvulnDuration);
        projectile.ConsumeHit();
    }

    private void TryEnemyContact(Collider2D other)
    {
        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy == null) return;

        enemy.TryContactDamage(this);
    }

    private void EnsureHurtboxTrigger()
    {
        CircleCollider2D[] colliders = GetComponents<CircleCollider2D>();
        foreach (CircleCollider2D col in colliders)
        {
            if (col.isTrigger)
                return;
        }

        CircleCollider2D hurtbox = gameObject.AddComponent<CircleCollider2D>();
        hurtbox.isTrigger = true;
        hurtbox.radius = 0.55f;
    }
}
