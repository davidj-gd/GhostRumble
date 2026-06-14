using UnityEngine;

/// <summary>
/// GhostMovement — WASD ice-physics + Q/E orbital + hold-Shift boost dash.
///
/// DASH BEHAVIOUR:
///   • Hold Shift to dash — increases movement speed / orbital spin speed.
///   • Dash gauge drains while Shift is held, refills while released.
///   • Gauge can be reused as soon as any fuel is available (no cooldown).
///   • During WASD: raises maxSpeed cap and adds extra forward force.
///   • During Q/E orbit: multiplies the orbital tangential force, making
///     the spin faster. Radial correction still holds the radius.
///   • HUD reads: DashGauge (0-1), IsDashing.
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
    [Header("Dash (Hold Shift)")]

    [Tooltip("Extra force added on top of moveForce while dashing during WASD.")]
    public float dashExtraForce = 20f;

    [Tooltip("How much higher the speed cap goes while dashing.")]
    public float dashSpeedBonus = 6f;

    [Tooltip("Multiplier applied to orbitForce while dashing during Q/E rotation. " +
             "e.g. 2.0 = twice as fast spin.")]
    public float dashOrbitMultiplier = 2.2f;

    [Tooltip("Seconds to drain the gauge from full to empty while holding Shift.")]
    public float dashDrainTime   = 2.5f;

    [Tooltip("Seconds to refill the gauge from empty to full while not dashing.")]
    public float dashRefillTime  = 3.5f;

    [Tooltip("Minimum gauge needed to START dashing (prevents flickering near 0).")]
    [Range(0f, 0.3f)]
    public float dashMinToStart  = 0.05f;

    [Header("Dash VFX")]
    [Tooltip("Empty child GameObject on the character. Place your particle system here.")]
    public Transform      dashVFXPoint;
    public ParticleSystem dashVFXPrefab;

    // ── Public state (read by GhostHUD) ───────────────────────────────────
    /// Gauge level 0 (empty) → 1 (full)
    public float DashGauge  { get; private set; } = 1f;
    /// True while Shift is held AND gauge is above zero
    public bool  IsDashing  { get; private set; }

    // ── Private ────────────────────────────────────────────────────────────
    Rigidbody rb;
    Vector3   inputDir;
    bool      isOrbiting;
    float     lockedOrbitRadius = -1f;
    bool            vfxPlaying;
    ParticleSystem  activeDashVFX;   // live prefab instance while dashing

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
        // ── Dash gauge ─────────────────────────────────────────────────────
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);

        if (shiftHeld && DashGauge >= dashMinToStart)
        {
            IsDashing   = true;
            DashGauge   = Mathf.Max(0f, DashGauge - Time.deltaTime / dashDrainTime);
            if (DashGauge <= 0f) IsDashing = false; // ran out mid-hold
        }
        else
        {
            IsDashing = false;
            DashGauge = Mathf.Min(1f, DashGauge + Time.deltaTime / dashRefillTime);
        }

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
    void HandleDashVFX()
    {
        if (dashVFXPoint == null) return;

        if (dashVFXPrefab != null)
        {
            // Spawn once on dash start, keep the reference, stop it on dash end
            if (IsDashing && !vfxPlaying)
            {
                vfxPlaying    = true;
                activeDashVFX = Instantiate(dashVFXPrefab, dashVFXPoint.position, dashVFXPoint.rotation);
                activeDashVFX.transform.SetParent(dashVFXPoint); // follows the character
                activeDashVFX.Play();
            }
            else if (!IsDashing && vfxPlaying)
            {
                vfxPlaying = false;
                if (activeDashVFX != null)
                {
                    activeDashVFX.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                    // Destroy after particles finish fading out
                    float fadeTime = activeDashVFX.main.startLifetime.constantMax + 0.2f;
                    Destroy(activeDashVFX.gameObject, fadeTime);
                    activeDashVFX = null;
                }
            }
        }
        else
        {
            // Child PS already on the character — just Play/Stop it directly
            ParticleSystem ex = dashVFXPoint.GetComponentInChildren<ParticleSystem>();
            if (ex == null) return;
            if (IsDashing && !ex.isPlaying)
                ex.Play();
            else if (!IsDashing && ex.isPlaying)
                ex.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
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
