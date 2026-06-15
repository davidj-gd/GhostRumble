using System.Collections;
using UnityEngine;

/// <summary>
/// GhostRumble — Stationary Boss Enemy
/// Inspired by Furi's bullet-hell phase design.
///
/// Stays fixed in place and fires bullet patterns on the X/Z plane.
/// Difficulty escalates in three stages based on how many times the
/// player has hit this enemy:
///
///   BEGINNER  (0  – hitsToIntermediate-1 hits)
///   INTERMEDIATE (hitsToIntermediate – hitsToHard-1 hits)
///   HARD      (hitsToHard+ hits)
///
/// Attach to any stationary GameObject. Assign the bulletPrefab and
/// (optionally) a shootOrigin child transform. Wire up PlayerHealth
/// events from GhostShooting / GhostProjectile to call RegisterHit().
/// </summary>
public class BossEnemy : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Inspector — Prefabs & References
    // ------------------------------------------------------------------ //

    [Header("References")]
    [Tooltip("Sphere projectile prefab. Should have a Rigidbody and Collider (no gravity).")]
    public GameObject bulletPrefab;

    [Tooltip("Optional child transform used as the bullet spawn origin. " +
             "If left empty the enemy's own transform is used.")]
    public Transform shootOrigin;

    [Tooltip("Optional particle effect spawned when this enemy is hit.")]
    public GameObject hitVFXPrefab;

    // ------------------------------------------------------------------ //
    //  Inspector — Hit Thresholds
    // ------------------------------------------------------------------ //

    [Header("Difficulty Thresholds (hits received)")]
    [Tooltip("Enemy enters INTERMEDIATE phase after this many hits.")]
    public int hitsToIntermediate = 5;

    [Tooltip("Enemy enters HARD phase after this many hits.")]
    public int hitsToHard = 12;

    // ------------------------------------------------------------------ //
    //  Inspector — Beginner Settings
    // ------------------------------------------------------------------ //

    [Header("Beginner Phase")]
    [Tooltip("Bullets fired per burst volley.")]
    public int beginnerBulletCount = 4;

    [Tooltip("Speed of each bullet (units/s).")]
    public float beginnerBulletSpeed = 6f;

    [Tooltip("Seconds between each full attack cycle.")]
    public float beginnerFireInterval = 1.8f;

    // ------------------------------------------------------------------ //
    //  Inspector — Intermediate Settings
    // ------------------------------------------------------------------ //

    [Header("Intermediate Phase")]
    public int intermediateBulletCount = 8;
    public float intermediateBulletSpeed = 9f;
    public float intermediateFireInterval = 1.2f;

    [Tooltip("When true a second wave fires slightly offset from the first.")]
    public bool intermediateDoubleWave = true;

    [Tooltip("Angular offset (degrees) of the second wave.")]
    public float intermediateWaveOffset = 22.5f;

    // ------------------------------------------------------------------ //
    //  Inspector — Hard Settings
    // ------------------------------------------------------------------ //

    [Header("Hard Phase")]
    public int hardBulletCount = 12;
    public float hardBulletSpeed = 13f;
    public float hardFireInterval = 0.75f;

    [Tooltip("Rotating burst: the pattern slowly spins each cycle.")]
    public bool hardRotatingBurst = true;

    [Tooltip("Degrees added to the burst angle each fire cycle.")]
    public float hardRotationStep = 15f;

    [Tooltip("A second ring fires inward (toward the enemy's position) simultaneously.")]
    public bool hardInwardRing = true;

    [Tooltip("Speed of the inward-travelling ring bullets.")]
    public float hardInwardSpeed = 7f;

    // ------------------------------------------------------------------ //
    //  Inspector — Shared
    // ------------------------------------------------------------------ //

    [Header("Shared")]
    [Tooltip("Lifetime of each bullet in seconds before it auto-destroys.")]
    public float bulletLifetime = 5f;

    [Tooltip("Seconds between individual bullets in a burst wave " +
             "(0 = all fire at once).")]
    public float burstSpacing = 0f;

    // ------------------------------------------------------------------ //
    //  Private State
    // ------------------------------------------------------------------ //

    private int _hitsReceived = 0;
    private float _rotationAccumulator = 0f;   // used by hard rotating burst
    private Coroutine _attackRoutine;

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle
    // ------------------------------------------------------------------ //

    private void Start()
    {
        // Freeze all position and rotation so the boss never moves.
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }

        if (shootOrigin == null)
            shootOrigin = transform;

        _attackRoutine = StartCoroutine(AttackLoop());
    }

    // ------------------------------------------------------------------ //
    //  Public API — call this from GhostProjectile / PlayerHealth events
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Call this whenever a player projectile hits this enemy.
    /// Wiring example (in GhostProjectile.cs OnCollisionEnter):
    ///   BossEnemy boss = other.GetComponent<BossEnemy>();
    ///   if (boss != null) boss.RegisterHit();
    /// </summary>
    public void RegisterHit()
    {
        _hitsReceived++;

        if (hitVFXPrefab != null)
            Instantiate(hitVFXPrefab, transform.position, Quaternion.identity);

        // Log every hit + phase transitions so you can confirm hits in console.
        if (_hitsReceived == hitsToIntermediate)
            Debug.Log("[BossEnemy] ★ Phase transition → INTERMEDIATE");
        else if (_hitsReceived == hitsToHard)
            Debug.Log("[BossEnemy] ★ Phase transition → HARD");
    }

    // ------------------------------------------------------------------ //
    //  Collision — detect player bullets hitting the boss directly
    // ------------------------------------------------------------------ //
    //
    //  This means you do NOT need to patch GhostProjectile.cs at all.
    //  The boss detects the hit itself when a Ball collider touches it.
    //  GhostProjectile.cs still handles damage to the player as normal.
    //
    private void OnCollisionEnter(Collision col)
    {
        // Ignore our own bullets hitting us.
        // (Physics.IgnoreCollision handles this already, but belt-and-suspenders.)
        if (col.gameObject == gameObject) return;

        // Accept anything that isn't another BossEnemy bullet.
        // Player bullets (Ball prefab) have no special tag needed —
        // if it hit us and it isn't our own bullet, count it.
        BossEnemy sourceBoss = col.gameObject.GetComponent<BossEnemy>();
        if (sourceBoss != null) return; // ignore boss-vs-boss if ever relevant

        // Check it's not one of our own spawned bullets by layer.
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (enemyBulletLayer != -1 && col.gameObject.layer == enemyBulletLayer) return;

        RegisterHit();
        Debug.Log($"[BossEnemy] Hit registered from: {col.gameObject.name} | " +
                  $"Total hits: {_hitsReceived} | Phase: {CurrentPhase()}");
    }

    // ------------------------------------------------------------------ //
    //  Phase Query
    // ------------------------------------------------------------------ //

    private enum Phase { Beginner, Intermediate, Hard }

    private Phase CurrentPhase()
    {
        if (_hitsReceived >= hitsToHard)     return Phase.Hard;
        if (_hitsReceived >= hitsToIntermediate) return Phase.Intermediate;
        return Phase.Beginner;
    }

    // ------------------------------------------------------------------ //
    //  Main Attack Loop
    // ------------------------------------------------------------------ //

    private IEnumerator AttackLoop()
    {
        // Small initial delay so the game has time to fully initialise.
        yield return new WaitForSeconds(0.5f);

        while (true)
        {
            switch (CurrentPhase())
            {
                case Phase.Beginner:
                    yield return StartCoroutine(BeginnerAttack());
                    yield return new WaitForSeconds(beginnerFireInterval);
                    break;

                case Phase.Intermediate:
                    yield return StartCoroutine(IntermediateAttack());
                    yield return new WaitForSeconds(intermediateFireInterval);
                    break;

                case Phase.Hard:
                    yield return StartCoroutine(HardAttack());
                    yield return new WaitForSeconds(hardFireInterval);
                    break;
            }
        }
    }

    // ------------------------------------------------------------------ //
    //  BEGINNER — simple equal-angle burst on the XZ plane
    // ------------------------------------------------------------------ //
    //
    //  Pattern:  4 bullets at 0°, 90°, 180°, 270°
    //            (evenly spread outward from the boss)
    //
    private IEnumerator BeginnerAttack()
    {
        FireRadialBurst(beginnerBulletCount, beginnerBulletSpeed, 0f);
        yield return null;
    }

    // ------------------------------------------------------------------ //
    //  INTERMEDIATE — dense ring + optional second offset wave
    // ------------------------------------------------------------------ //
    //
    //  Pattern:  8 bullets equally spaced (45° apart)
    //            + 8 more rotated by intermediateWaveOffset degrees
    //
    private IEnumerator IntermediateAttack()
    {
        FireRadialBurst(intermediateBulletCount, intermediateBulletSpeed, 0f);

        if (intermediateDoubleWave)
        {
            if (burstSpacing > 0f)
                yield return new WaitForSeconds(burstSpacing);

            FireRadialBurst(intermediateBulletCount, intermediateBulletSpeed,
                            intermediateWaveOffset);
        }

        yield return null;
    }

    // ------------------------------------------------------------------ //
    //  HARD — rotating burst + optional inward ring
    // ------------------------------------------------------------------ //
    //
    //  Pattern:  12 bullets rotating each cycle (Furi-style spiral feel)
    //            + 12 bullets travelling INWARD simultaneously
    //
    private IEnumerator HardAttack()
    {
        // Outward rotating burst
        FireRadialBurst(hardBulletCount, hardBulletSpeed, _rotationAccumulator);

        // Inward-converging ring (bullets aimed at the boss's own position)
        if (hardInwardRing)
        {
            if (burstSpacing > 0f)
                yield return new WaitForSeconds(burstSpacing);

            FireInwardRing(hardBulletCount, hardInwardSpeed, _rotationAccumulator);
        }

        // Advance the rotation for next cycle
        if (hardRotatingBurst)
            _rotationAccumulator += hardRotationStep;

        yield return null;
    }

    // ------------------------------------------------------------------ //
    //  Bullet Helpers
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Fires <count> bullets equally spaced around 360° on the XZ plane,
    /// all travelling outward from the boss.
    /// <offsetDegrees> rotates the whole pattern.
    /// </summary>
    private void FireRadialBurst(int count, float speed, float offsetDegrees)
    {
        float step = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angleDeg = (i * step) + offsetDegrees;
            Vector3 dir = AngleToXZDirection(angleDeg);
            SpawnBullet(shootOrigin.position, dir, speed);
        }
    }

    /// <summary>
    /// Fires <count> bullets equally spaced around 360° but aimed INWARD
    /// (toward the boss's position). Useful for creating converging rings
    /// that reward the player for staying mobile.
    /// </summary>
    private void FireInwardRing(int count, float speed, float offsetDegrees)
    {
        float radius = 8f;  // Bullets spawn this far out and travel inward.
        float step   = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float angleDeg = (i * step) + offsetDegrees;
            Vector3 dir    = AngleToXZDirection(angleDeg);

            // Spawn point is displaced outward; direction is reversed (inward).
            Vector3 spawnPos = shootOrigin.position + dir * radius;
            Vector3 inward   = -dir;

            SpawnBullet(spawnPos, inward, speed);
        }
    }

    /// <summary>
    /// Instantiates the bulletPrefab, sets its velocity on the XZ plane,
    /// and schedules destruction after bulletLifetime seconds.
    /// </summary>
    private void SpawnBullet(Vector3 position, Vector3 direction, float speed)
    {
        if (bulletPrefab == null)
        {
            Debug.LogWarning("[BossEnemy] bulletPrefab is not assigned!");
            return;
        }

        // Flatten direction to XZ — no vertical travel.
        direction.y = 0f;
        direction.Normalize();

        GameObject bullet = Instantiate(bulletPrefab, position, Quaternion.identity);

        Rigidbody rb = bullet.GetComponent<Rigidbody>();
        if (rb == null)
            rb = bullet.AddComponent<Rigidbody>();

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY |
                         RigidbodyConstraints.FreezeRotationX |
                         RigidbodyConstraints.FreezeRotationZ;
        rb.linearVelocity = direction * speed;

        // Ignore collision between boss bullets and the boss itself.
        Collider bossCol  = GetComponent<Collider>();
        Collider bulletCol = bullet.GetComponent<Collider>();
        if (bossCol != null && bulletCol != null)
            Physics.IgnoreCollision(bulletCol, bossCol);

        // Put bullet on its own physics layer so boss bullets never
        // collide with each other. Layer 8 = "EnemyBullet" (add this
        // layer in Edit > Project Settings > Tags and Layers).
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (enemyBulletLayer != -1)
            bullet.layer = enemyBulletLayer;

        Destroy(bullet, bulletLifetime);
    }

    /// <summary>
    /// Converts a flat angle in degrees to a normalised XZ direction vector.
    /// 0° = +X axis, angles increase counter-clockwise when viewed from above.
    /// </summary>
    private static Vector3 AngleToXZDirection(float degrees)
    {
        float rad = degrees * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
    }

    // ------------------------------------------------------------------ //
    //  Gizmos — visualise bullet directions in the Scene view
    // ------------------------------------------------------------------ //

    private void OnDrawGizmosSelected()
    {
        if (shootOrigin == null) return;

        // Draw current phase pattern preview.
        int   count    = beginnerBulletCount;
        float step     = 360f / count;
        float offset   = 0f;

#if UNITY_EDITOR
        Phase phase = Application.isPlaying ? CurrentPhase() : Phase.Beginner;

        switch (phase)
        {
            case Phase.Intermediate:
                count  = intermediateBulletCount;
                Gizmos.color = Color.yellow;
                break;
            case Phase.Hard:
                count  = hardBulletCount;
                offset = _rotationAccumulator;
                Gizmos.color = Color.red;
                break;
            default:
                Gizmos.color = Color.cyan;
                break;
        }
#endif

        step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float   angleDeg = (i * step) + offset;
            Vector3 dir      = AngleToXZDirection(angleDeg);
            Gizmos.DrawRay(shootOrigin.position, dir * 2f);
        }
    }
}
