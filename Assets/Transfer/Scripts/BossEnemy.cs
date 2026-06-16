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

    [Tooltip("The player transform the fan pattern aims at. Assign Player 1 here.")]
    public Transform playerTarget;

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
    [Tooltip("Number of bullets in the fan spread toward the player.")]
    public int beginnerBulletCount = 5;

    [Tooltip("Total angle of the fan spread in degrees. 60 = wide, 30 = tight.")]
    public float beginnerFanSpread = 60f;

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
        else
        {
            // Boss needs a Rigidbody for OnCollisionEnter to fire.
            rb = gameObject.AddComponent<Rigidbody>();
            rb.useGravity  = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            Debug.Log("[BossEnemy] Rigidbody added automatically.");
        }

        // Ensure a non-trigger collider exists for OnCollisionEnter.
        // Also add a slightly larger trigger collider as a fallback catch-all.
        Collider existing = GetComponent<Collider>();
        if (existing == null)
        {
            SphereCollider sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 1f;
            sc.isTrigger = false;
            Debug.Log("[BossEnemy] SphereCollider (non-trigger, r=1) added automatically.");
        }

        // Add a trigger collider one size larger as a belt-and-suspenders fallback.
        // This catches cases where the player bullet passes through on fast shots.
        SphereCollider trigger = gameObject.AddComponent<SphereCollider>();
        trigger.radius    = (existing != null) ? 1.2f : 1.2f;
        trigger.isTrigger = true;

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
    //  Collision & Trigger — detect player bullets hitting the boss
    // ------------------------------------------------------------------ //
    //
    //  Both OnCollisionEnter (non-trigger collider) and OnTriggerEnter
    //  (trigger collider) are handled. Whichever fires first wins.
    //  A frame-based guard (_hitRegisteredThisFrame) prevents a single
    //  bullet from counting twice if it touches both colliders at once.
    //
    private int _lastHitFrame = -1;

    private void OnCollisionEnter(Collision col)
    {
        TryRegisterHit(col.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        TryRegisterHit(other.gameObject);
    }

    private void TryRegisterHit(GameObject source)
    {
        // Deduplicate: ignore if we already registered a hit this frame.
        if (Time.frameCount == _lastHitFrame) return;

        // Ignore ourselves.
        if (source == gameObject) return;

        // Ignore our own spawned enemy bullets by layer.
        int enemyBulletLayer = LayerMask.NameToLayer("EnemyBullet");
        if (enemyBulletLayer != -1 && source.layer == enemyBulletLayer) return;

        // Ignore other BossEnemy objects.
        if (source.GetComponent<BossEnemy>() != null) return;

        // Anything else that touches us counts as a player hit.
        _lastHitFrame = Time.frameCount;
        RegisterHit();
        Debug.Log($"[BossEnemy] Hit! Source: {source.name} | " +
                  $"Total: {_hitsReceived} | Phase: {CurrentPhase()}");
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
    //  BEGINNER — 5-bullet fan spread aimed at the player
    // ------------------------------------------------------------------ //
    //
    //  Pattern:  5 bullets fanning out toward the player's current position.
    //            The centre bullet aims directly at the player; the outer
    //            two bullets are spread by beginnerFanSpread/2 on each side.
    //
    private IEnumerator BeginnerAttack()
    {
        FireFanBurst(beginnerBulletCount, beginnerBulletSpeed, beginnerFanSpread);
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
    /// Fires <count> bullets in a fan spread aimed at the player.
    /// The centre bullet points directly at playerTarget; the remaining
    /// bullets are distributed evenly across totalSpreadDegrees.
    /// Falls back to a forward-facing fan if playerTarget is not assigned.
    /// </summary>
    private void FireFanBurst(int count, float speed, float totalSpreadDegrees)
    {
        // Aim the centre of the fan at the player.
        Vector3 toPlayer;
        if (playerTarget != null)
        {
            toPlayer = playerTarget.position - shootOrigin.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f)
                toPlayer = transform.forward;
            toPlayer.Normalize();
        }
        else
        {
            toPlayer = transform.forward;
            Debug.LogWarning("[BossEnemy] playerTarget not assigned — fan aims forward.");
        }

        // Convert the centre direction to an angle on the XZ plane.
        float centreAngle = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;

        // Spread bullets evenly across the fan.
        float halfSpread = totalSpreadDegrees * 0.5f;
        float step       = (count > 1) ? totalSpreadDegrees / (count - 1) : 0f;

        for (int i = 0; i < count; i++)
        {
            float offset    = (count > 1) ? -halfSpread + (i * step) : 0f;
            float angleDeg  = centreAngle + offset;
            Vector3 dir     = AngleToXZDirection(angleDeg);
            SpawnBullet(shootOrigin.position, dir, speed);
        }
    }

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

        // Rotate bullet to face its travel direction.
        Quaternion bulletRotation = direction != Vector3.zero
            ? Quaternion.LookRotation(direction, Vector3.up)
            : Quaternion.identity;

        GameObject bullet = Instantiate(bulletPrefab, position, bulletRotation);

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

#if UNITY_EDITOR
        Phase phase = Application.isPlaying ? CurrentPhase() : Phase.Beginner;

        switch (phase)
        {
            case Phase.Beginner:
            {
                Gizmos.color = Color.cyan;
                // Draw fan aimed at player (or forward if not assigned).
                Vector3 toPlayer = (playerTarget != null)
                    ? (playerTarget.position - shootOrigin.position)
                    : transform.forward;
                toPlayer.y = 0f;
                if (toPlayer.sqrMagnitude > 0.001f) toPlayer.Normalize();
                float centreAngle = Mathf.Atan2(toPlayer.z, toPlayer.x) * Mathf.Rad2Deg;
                float halfSpread  = beginnerFanSpread * 0.5f;
                float step        = (beginnerBulletCount > 1)
                    ? beginnerFanSpread / (beginnerBulletCount - 1) : 0f;
                for (int i = 0; i < beginnerBulletCount; i++)
                {
                    float offset   = (beginnerBulletCount > 1) ? -halfSpread + (i * step) : 0f;
                    Vector3 dir    = AngleToXZDirection(centreAngle + offset);
                    Gizmos.DrawRay(shootOrigin.position, dir * 2f);
                }
                break;
            }
            case Phase.Intermediate:
            {
                Gizmos.color = Color.yellow;
                float step = 360f / intermediateBulletCount;
                for (int i = 0; i < intermediateBulletCount; i++)
                {
                    Vector3 dir = AngleToXZDirection(i * step);
                    Gizmos.DrawRay(shootOrigin.position, dir * 2f);
                }
                break;
            }
            case Phase.Hard:
            {
                Gizmos.color = Color.red;
                float step = 360f / hardBulletCount;
                for (int i = 0; i < hardBulletCount; i++)
                {
                    Vector3 dir = AngleToXZDirection((i * step) + _rotationAccumulator);
                    Gizmos.DrawRay(shootOrigin.position, dir * 2f);
                }
                break;
            }
        }
#endif
    }
}
