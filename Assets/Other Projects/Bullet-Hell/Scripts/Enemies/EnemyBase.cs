using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Shared enemy chase logic. Visuals pulse on idle; attacks use UnitVisuals sheets.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Health))]
public abstract class EnemyBase : MonoBehaviour
{
    private const float SpriteFacingOffset = -90f;

    [Header("Movement")]
    public float moveSpeed = 2f;

    [Header("Contact Damage")]
    public float contactDamage = 5f;
    public float contactDamageCooldown = 0.5f;

    [Header("Visuals")]
    public AttackAnimationData animationData;
    public bool rotateVisualToTarget;

    protected Transform player;
    protected Rigidbody2D rb;
    protected Health health;
    protected UnitVisuals combatVisuals;
    protected Transform aimPivot;
    protected Transform firePoint;

    private float contactDamageTimer;

    protected bool IsPerformingAttack { get; private set; }

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        EnsureVisualChild();
    }

    protected virtual void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;

        health.OnDeath += HandleDeath;

        if (GameplayTuning.Instance != null)
            GameplayTuning.Instance.ApplyEnemyScale(transform);

        if (combatVisuals != null && animationData != null)
            combatVisuals.Configure(animationData);

        if (GameplayTuning.Instance != null)
            GameplayTuning.Instance.ApplyToUnitVisuals(combatVisuals);
    }

    protected virtual void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= HandleDeath;
    }

    protected virtual void Update()
    {
        if (contactDamageTimer > 0f)
            contactDamageTimer -= Time.deltaTime;

        if (rotateVisualToTarget && player != null && !IsPerformingAttack)
            AimAtPlayer();
    }

    protected virtual void FixedUpdate()
    {
        if (player == null || health.IsDead || IsPerformingAttack)
            return;

        UpdateMovement();
    }

    public void TryContactDamage(PlayerHurtHandler playerHurt)
    {
        if (contactDamageTimer > 0f || health.IsDead || playerHurt == null) return;
        if (!playerHurt.CanTakeDamage) return;

        playerHurt.ReceiveDamage(contactDamage, contactDamageCooldown);
        contactDamageTimer = contactDamageCooldown;
    }

    protected virtual void UpdateMovement()
    {
        Vector2 dir = (Vector2)player.position - rb.position;
        if (dir.sqrMagnitude < 0.001f) return;

        dir.Normalize();
        rb.linearVelocity = dir * moveSpeed;
    }

    protected void PlayAttackThen(Action onComplete)
    {
        rb.linearVelocity = Vector2.zero;

        if (combatVisuals == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (animationData != null)
            combatVisuals.Configure(animationData);

        IsPerformingAttack = true;
        combatVisuals.PlayAttack(animationData, () =>
        {
            IsPerformingAttack = false;
            onComplete?.Invoke();
        });
    }

    protected void AimAtPlayer()
    {
        if (aimPivot == null) return;

        Vector2 dir = DirectionToPlayer();
        float aimAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle + SpriteFacingOffset);
    }

    protected Vector2 DirectionToPlayer()
    {
        if (player == null) return Vector2.right;
        Vector2 dir = (Vector2)player.position - rb.position;
        return dir.sqrMagnitude < 0.001f ? Vector2.right : dir.normalized;
    }

    protected float DistanceToPlayer()
    {
        return player == null ? float.MaxValue : Vector2.Distance(rb.position, player.position);
    }

    protected void SpawnProjectile(
        GameObject prefab,
        Vector2 direction,
        float projectileSpeed,
        float projectileDamage,
        float scale = 1f,
        bool homing = false)
    {
        if (prefab == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;

        Projectile prefabProjectile = prefab.GetComponent<Projectile>();
        if (prefabProjectile != null)
            CombatEffects.PlayMuzzleFlash(prefabProjectile.vfx, spawnPos, rotation);

        GameObject bullet = Instantiate(prefab, spawnPos, rotation);
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(Team.Enemy, projectileDamage, projectileSpeed, scale, homing, player, 260f);
    }

    protected virtual void HandleDeath()
    {
        Destroy(gameObject);
    }

    private void EnsureVisualChild()
    {
        combatVisuals = GetComponentInChildren<UnitVisuals>(true);
        if (combatVisuals == null)
        {
            SpriteRenderer rootRenderer = GetComponent<SpriteRenderer>();
            GameObject visualGo = new GameObject("Visual");
            visualGo.transform.SetParent(transform, false);

            SpriteRenderer visualRenderer = visualGo.AddComponent<SpriteRenderer>();
            if (rootRenderer != null)
            {
                visualRenderer.sprite = rootRenderer.sprite;
                visualRenderer.color = rootRenderer.color;
                visualRenderer.sortingLayerID = rootRenderer.sortingLayerID;
                visualRenderer.sortingOrder = rootRenderer.sortingOrder;
                Destroy(rootRenderer);
            }

            combatVisuals = visualGo.AddComponent<UnitVisuals>();
            combatVisuals.animationData = animationData;
        }

        aimPivot = combatVisuals.transform;

        firePoint = aimPivot.Find("FirePoint");
        if (firePoint == null)
        {
            GameObject fp = new GameObject("FirePoint");
            fp.transform.SetParent(aimPivot, false);
            fp.transform.localPosition = Vector3.zero;
            firePoint = fp.transform;
        }
        else
        {
            firePoint.SetParent(aimPivot, false);
            firePoint.localPosition = Vector3.zero;
        }
    }
}
