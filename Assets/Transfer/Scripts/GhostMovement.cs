using UnityEngine;

/// <summary>
/// GhostMovement — WASD ice-physics + Q/E orbital + hold-Shift boost dash.
///
/// DASH BEHAVIOUR:
///   • Hold Shift to dash — increases movement speed / orbital spin speed.
///   • Dash is infinite — no gauge, no drain, no cooldown.
///   • During WASD: raises maxSpeed cap and adds extra forward force.
///   • During Q/E orbit: multiplies the orbital tangential force, making
///     the spin faster. Radial correction still holds the radius.
///   • VFX spawns on the OPPOSITE side of the movement direction (trail feel).
///   • HUD reads: IsDashing (DashGauge always returns 1 for compatibility).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GhostMovement : MonoBehaviour
{
    // ── WASD ───────────────────────────────────────────────────────────────
    [Header("WASD Movement")]
    public float moveForce  = 22f;
    public float maxSpeed   = 9f;
    [Range(0f, 5f)] public float idleDrag   = 1.2f;
    [Range(0f, 2f)] public float movingDrag = 0.05f;

    // ── Tilt ───────────────────────────────────────────────────────────────
    [Header("Tilt")]
    public float maxTiltAngle = 14f;
    public float tiltSpeed    = 7f;

    // ── Orbital ────────────────────────────────────────────────────────────
    [Header("Q/E Orbital Slide")]
    public Transform orbitTarget;
    public float     orbitForce = 28f;

    [Tooltip("0 = locked radius. + = spiral out. - = spiral in.")]
    [Range(-20f, 20f)]
    public float orbitRadialDrift = 0f;

    [Tooltip("Spring strength keeping player on the orbit radius.")]
    public float orbitRadiusStiffness = 18f;

    // ── Dash ───────────────────────────────────────────────────────────────
    [Header("Dash (Hold Shift) — Infinite")]

    [Tooltip("Extra force added on top of moveForce while dashing during WASD.")]
    public float dashExtraForce = 20f;

    [Tooltip("How much higher the speed cap goes while dashing.")]
    public float dashSpeedBonus = 6f;

    [Tooltip("Multiplier applied to orbitForce while dashing during Q/E rotation. " +
             "e.g. 2.0 = twice as fast spin.")]
    public float dashOrbitMultiplier = 2.2f;

    [Header("Dash VFX")]
    [Tooltip("The spawn point child transform on the character (empty GO). Place it where you want the effect to appear.")]
    public Transform  dashVFXPoint;

    [Tooltip("Prefab to show while dashing. Can be a ParticleSystem prefab OR any mesh/GameObject prefab.\n\n" +
             "• ParticleSystem prefab — played on dash start, stopped on dash end (with natural fade-out).\n" +
             "• Mesh / any other prefab — instantiated on dash start, destroyed instantly on dash end.\n" +
             "• Leave empty — the script will look for a ParticleSystem already sitting under dashVFXPoint.")]
    public GameObject dashVFXPrefab;

    // ── Public state (read by GhostHUD) ───────────────────────────────────
    /// Always 1 — dash is infinite. Kept for HUD compatibility.
    public float DashGauge  { get; private set; } = 1f;
    /// True while Shift is held
    public bool  IsDashing  { get; private set; }

    // ── Private ────────────────────────────────────────────────────────────
    Rigidbody rb;
    Vector3   inputDir;
    bool      isOrbiting;
    float     lockedOrbitRadius = -1f;
    bool            vfxPlaying;
    GameObject      activeDashVFXInstance;  // live instance while dashing
    ParticleSystem  activeDashVFXPS;        // cached PS on the instance, if any

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // ── Dash — infinite, no gauge ──────────────────────────────────────
        IsDashing = Input.GetKey(KeyCode.LeftShift);

        HandleDashVFX();

        // ── Q/E orbit ──────────────────────────────────────────────────────
        bool q = Input.GetKey(KeyCode.Q);
        bool e = Input.GetKey(KeyCode.E);
        isOrbiting = (q || e) && orbitTarget != null;

        if (isOrbiting)
        {
            if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E))
            {
                Vector3 toPlayer = transform.position - orbitTarget.position;
                toPlayer.y = 0f;
                lockedOrbitRadius = toPlayer.magnitude;
            }
            ApplyOrbitalForce(q ? 1f : -1f);
            rb.linearDamping = movingDrag;
            inputDir = Vector3.zero;
        }
        else
        {
            lockedOrbitRadius = -1f;
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            inputDir = new Vector3(h, 0f, v).normalized;
            rb.linearDamping = inputDir.sqrMagnitude > 0f ? movingDrag : idleDrag;
        }
    }

    void FixedUpdate()
    {
        if (!isOrbiting && inputDir.sqrMagnitude > 0f)
        {
            float force = IsDashing ? moveForce + dashExtraForce : moveForce;
            rb.AddForce(inputDir * force, ForceMode.Force);
        }

        // Speed cap — raised while dashing
        float cap = IsDashing ? maxSpeed + dashSpeedBonus : maxSpeed;
        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flat.magnitude > cap)
        {
            Vector3 capped = flat.normalized * cap;
            rb.linearVelocity = new Vector3(capped.x, rb.linearVelocity.y, capped.z);
        }

        TiltTowardVelocity();
    }

    // ── Orbital ────────────────────────────────────────────────────────────
    void ApplyOrbitalForce(float direction)
    {
        if (orbitTarget == null) return;

        Vector3 toPlayer = transform.position - orbitTarget.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return;

        float   currentRadius = toPlayer.magnitude;
        Vector3 radialDir     = toPlayer.normalized;

        Vector3 tangent = direction > 0f
            ? new Vector3( toPlayer.z, 0f, -toPlayer.x).normalized
            : new Vector3(-toPlayer.z, 0f,  toPlayer.x).normalized;

        // Dash multiplies the spin force — starts orbiting faster immediately
        float appliedForce = IsDashing ? orbitForce * dashOrbitMultiplier : orbitForce;
        rb.AddForce(tangent * appliedForce, ForceMode.Force);

        if (lockedOrbitRadius > 0f)
        {
            float targetRadius = lockedOrbitRadius + orbitRadialDrift * Time.deltaTime;
            lockedOrbitRadius  = targetRadius;
            float radiusError  = currentRadius - targetRadius;
            rb.AddForce(-radialDir * radiusError * orbitRadiusStiffness, ForceMode.Force);
        }
    }

    // ── Dash VFX ───────────────────────────────────────────────────────────
    //
    //  The VFX spawns on the OPPOSITE side of the player's movement direction
    //  so it reads as a motion trail rather than a forward effect.
    //  dashVFXPoint still defines the offset distance from the character —
    //  its local position is mirrored behind the velocity direction at runtime.
    //
    //  Three cases, evaluated in order:
    //
    //  1. dashVFXPrefab assigned + ParticleSystem on it
    //     → Instantiate on dash start (behind movement), stop with fade on end.
    //
    //  2. dashVFXPrefab assigned + no ParticleSystem (mesh / generic GO)
    //     → Instantiate on dash start, destroy immediately on dash end.
    //     → Position updates every frame to stay behind the moving character.
    //
    //  3. dashVFXPrefab null
    //     → Drive a ParticleSystem already childed under dashVFXPoint directly.
    //
    void HandleDashVFX()
    {
        if (dashVFXPoint == null) return;

        // Compute the spawn/follow position: behind the velocity direction.
        // Uses the dashVFXPoint's distance from the character root as the offset.
        Vector3 trailPosition = GetTrailPosition();

        if (dashVFXPrefab != null)
        {
            if (IsDashing && !vfxPlaying)
            {
                vfxPlaying            = true;
                activeDashVFXInstance = Instantiate(dashVFXPrefab, trailPosition,
                                                    dashVFXPoint.rotation);
                activeDashVFXInstance.transform.SetParent(dashVFXPoint);

                activeDashVFXPS = activeDashVFXInstance.GetComponent<ParticleSystem>();
                if (activeDashVFXPS == null)
                    activeDashVFXPS = activeDashVFXInstance.GetComponentInChildren<ParticleSystem>();

                if (activeDashVFXPS != null)
                    activeDashVFXPS.Play();
            }
            else if (IsDashing && vfxPlaying && activeDashVFXPS == null)
            {
                // Mesh/generic prefab: move it every frame to stay behind the player.
                if (activeDashVFXInstance != null)
                    activeDashVFXInstance.transform.position = trailPosition;
            }
            else if (!IsDashing && vfxPlaying)
            {
                vfxPlaying = false;

                if (activeDashVFXInstance != null)
                {
                    if (activeDashVFXPS != null)
                    {
                        activeDashVFXPS.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                        float fadeTime = activeDashVFXPS.main.startLifetime.constantMax + 0.2f;
                        Destroy(activeDashVFXInstance, fadeTime);
                    }
                    else
                    {
                        Destroy(activeDashVFXInstance);
                    }

                    activeDashVFXInstance = null;
                    activeDashVFXPS       = null;
                }
            }
        }
        else
        {
            // No prefab — drive child PS directly. Move dashVFXPoint to trail pos.
            dashVFXPoint.position = trailPosition;

            ParticleSystem ex = dashVFXPoint.GetComponentInChildren<ParticleSystem>();
            if (ex == null) return;
            if (IsDashing && !ex.isPlaying)
                ex.Play();
            else if (!IsDashing && ex.isPlaying)
                ex.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
    }

    /// <summary>
    /// Returns the world position on the OPPOSITE side of the player's
    /// current movement direction, at the same distance dashVFXPoint sits
    /// from the character root in local space.
    /// Falls back to dashVFXPoint's world position when the player is still.
    /// </summary>
    Vector3 GetTrailPosition()
    {
        // Distance from character root to dashVFXPoint (used as trail offset).
        float offsetDist = dashVFXPoint.localPosition.magnitude;
        if (offsetDist < 0.001f) offsetDist = 0.5f; // safe default

        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flat.sqrMagnitude > 0.01f)
        {
            // Behind = opposite of velocity direction.
            Vector3 behind = -flat.normalized;
            return transform.position + behind * offsetDist;
        }

        // Player is stationary — keep at current dashVFXPoint world position.
        return dashVFXPoint.position;
    }

    // ── Tilt ───────────────────────────────────────────────────────────────
    void TiltTowardVelocity()
    {
        Vector3 flat      = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float   speedFrac = Mathf.Clamp01(flat.magnitude / (maxSpeed + dashSpeedBonus));
        Quaternion targetRot = flat.sqrMagnitude > 0.01f
            ? Quaternion.AngleAxis(maxTiltAngle * speedFrac, Vector3.Cross(Vector3.up, flat.normalized))
            : Quaternion.identity;
        Quaternion yOnly = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, yOnly * targetRot, Time.fixedDeltaTime * tiltSpeed);
    }
}
