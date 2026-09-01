using UnityEngine;

/// <summary>
/// Straight or homing projectile with particle travel VFX and impact effects.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Projectile : MonoBehaviour
{
    public float lifeTime = 3f;
    public ProjectileVFXConfig vfx;

    [Header("Legacy Sprite (optional fallback)")]
    public ProjectileAnimationData travelAnimation;

    private Team ownerTeam;
    private float damage;
    private float speed;
    private bool consumed;
    private bool homing;
    private Transform homingTarget;
    private float homingTurnRate = 240f;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private SpriteFramePlayer framePlayer;
    private ParticleSystem travelParticles;
    private GameObject spawnedTravelParticleRoot;

    public float DamageAmount => damage;
    public bool CanHitPlayer => ownerTeam == Team.Enemy && !consumed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        framePlayer = GetComponent<SpriteFramePlayer>();
        travelParticles = GetComponentInChildren<ParticleSystem>();
    }

    public void Init(
        Team team,
        float damage,
        float speed,
        float scale = 1f,
        bool enableHoming = false,
        Transform target = null,
        float turnRate = 240f)
    {
        ownerTeam = team;
        this.damage = damage;
        this.speed = speed;
        homing = enableHoming;
        homingTarget = target;
        homingTurnRate = turnRate;

        transform.localScale = Vector3.one * scale;
        rb.linearVelocity = transform.right * speed;

        SetupTravelVisuals();
        Destroy(gameObject, lifeTime);
    }

    private void SetupTravelVisuals()
    {
        bool usingParticles = false;

        if (vfx != null && vfx.travelParticlePrefab != null)
        {
            spawnedTravelParticleRoot = Instantiate(vfx.travelParticlePrefab, transform);
            spawnedTravelParticleRoot.transform.localPosition = Vector3.zero;
            spawnedTravelParticleRoot.transform.localRotation = Quaternion.identity;
            travelParticles = spawnedTravelParticleRoot.GetComponent<ParticleSystem>()
                ?? spawnedTravelParticleRoot.GetComponentInChildren<ParticleSystem>();
            usingParticles = travelParticles != null;
        }
        else if (travelParticles != null)
        {
            usingParticles = true;
        }

        if (usingParticles && travelParticles != null)
        {
            travelParticles.Play(true);
            if (vfx == null || vfx.hideSpriteRenderer)
                SetSpriteVisible(false);
            return;
        }

        if (travelAnimation != null && framePlayer != null)
            framePlayer.Play(travelAnimation.travelFrames, travelAnimation.frameRate);
    }

    private void FixedUpdate()
    {
        if (consumed || !homing || homingTarget == null)
            return;

        Vector2 desired = ((Vector2)homingTarget.position - rb.position).normalized;
        Vector2 current = rb.linearVelocity.normalized;
        if (current.sqrMagnitude < 0.001f)
            current = (Vector2)transform.right;

        float maxRadians = homingTurnRate * Mathf.Deg2Rad * Time.fixedDeltaTime;
        Vector3 newDir = Vector3.RotateTowards(current, desired, maxRadians, 0f);
        rb.linearVelocity = (Vector2)newDir * speed;
        transform.right = newDir;
    }

    public void ConsumeHit()
    {
        if (consumed) return;
        consumed = true;

        CombatEffects.PlayImpact(vfx, transform.position, transform.rotation);
        StopTravelVisuals();
        Destroy(gameObject);
    }

    private void StopTravelVisuals()
    {
        if (travelParticles != null)
            travelParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);

        if (spawnedTravelParticleRoot != null)
            Destroy(spawnedTravelParticleRoot);
    }

    private void SetSpriteVisible(bool visible)
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = visible;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (consumed) return;
        if (other.GetComponent<Projectile>() != null) return;

        Health targetHealth = other.GetComponentInParent<Health>();
        if (targetHealth != null)
        {
            if (targetHealth.team == ownerTeam) return;
            if (targetHealth.team == Team.Player) return;

            targetHealth.TakeDamage(damage);
            ConsumeHit();
            return;
        }

        if (!other.isTrigger)
            ConsumeHit();
    }
}
