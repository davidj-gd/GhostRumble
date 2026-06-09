using UnityEngine;
using System.Collections;

/// <summary>
/// GhostMovement — WASD ice-physics + Q/E orbital + Shift dash (3 stacks).
/// Exposes dash stack state so the HUD can read it.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class GhostMovement : MonoBehaviour
{
    // ── WASD ───────────────────────────────────────────────────────────────
    [Header("WASD Movement")]
    public float moveForce  = 22f;
    public float maxSpeed   = 9f;
    [Range(0f,5f)] public float idleDrag   = 1.2f;
    [Range(0f,2f)] public float movingDrag = 0.05f;

    // ── Tilt ───────────────────────────────────────────────────────────────
    [Header("Tilt")]
    public float maxTiltAngle = 14f;
    public float tiltSpeed    = 7f;

    // ── Orbital ────────────────────────────────────────────────────────────
    [Header("Q/E Orbital Slide")]
    public Transform orbitTarget;
    public float     orbitForce = 28f;

    [Tooltip("How strongly the player is pulled/pushed radially while orbiting.\n" +
             "0  = locked radius (perfect circle)\n" +
             "> 0 = orbit gradually moves outward (spiral out)\n" +
             "< 0 = orbit gradually moves inward (spiral in)")]
    [Range(-20f, 20f)]
    public float orbitRadialDrift = 0f;

    [Tooltip("How hard the radius correction force pulls the player back to the locked orbit distance. " +
             "Higher = tighter circle. Lower = looser/driftier arc.")]
    public float orbitRadiusStiffness = 18f;

    // ── Dash ───────────────────────────────────────────────────────────────
    [Header("Dash (Shift)")]
    public float dashForce    = 18f;
    public float dashCooldown = 1f;     // seconds to regain one stack
    public int   maxDashStacks = 3;

    [Tooltip("Empty GameObject on the character where the dash VFX spawns.")]
    public Transform      dashVFXPoint;
    public ParticleSystem dashVFXPrefab;

    // ── public read (for HUD) ──────────────────────────────────────────────
    public int   DashStacks      { get; private set; }
    /// 0‒1 fill of the stack currently recharging (0 = just used, 1 = full)
    public float RechargeProgress { get; private set; } = 1f;

    // ── private ────────────────────────────────────────────────────────────
    Rigidbody rb;
    Vector3   inputDir;
    bool      isOrbiting;
    float     rechargeTimer;
    float     lockedOrbitRadius = -1f;  // captured when Q/E first pressed

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity  = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY
                       | RigidbodyConstraints.FreezeRotationX
                       | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        DashStacks = maxDashStacks;
    }

    void Update()
    {
        // ── Recharge ───────────────────────────────────────────────────────
        if (DashStacks < maxDashStacks)
        {
            rechargeTimer += Time.deltaTime;
            RechargeProgress = Mathf.Clamp01(rechargeTimer / dashCooldown);
            if (rechargeTimer >= dashCooldown)
            {
                DashStacks++;
                rechargeTimer = 0f;
                RechargeProgress = DashStacks < maxDashStacks ? 0f : 1f;
            }
        }

        // ── Q/E orbit ──────────────────────────────────────────────────────
        bool q = Input.GetKey(KeyCode.Q);
        bool e = Input.GetKey(KeyCode.E);
        isOrbiting = (q || e) && orbitTarget != null;

        if (isOrbiting)
        {
            // Capture radius the first frame Q/E is pressed
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
            lockedOrbitRadius = -1f;  // reset so next press re-captures cleanly
            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");
            inputDir = new Vector3(h, 0f, v).normalized;
            rb.linearDamping = inputDir.sqrMagnitude > 0f ? movingDrag : idleDrag;
        }

        // ── Dash ───────────────────────────────────────────────────────────
        if (Input.GetKeyDown(KeyCode.LeftShift) && DashStacks > 0)
            StartCoroutine(Dash());
    }

    void FixedUpdate()
    {
        if (!isOrbiting && inputDir.sqrMagnitude > 0f)
            rb.AddForce(inputDir * moveForce, ForceMode.Force);

        Vector3 flat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (flat.magnitude > maxSpeed)
        {
            Vector3 capped = flat.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(capped.x, rb.linearVelocity.y, capped.z);
        }

        TiltTowardVelocity();
    }

    IEnumerator Dash()
    {
        DashStacks--;
        if (DashStacks < maxDashStacks - 1 == false) // first stack used
            rechargeTimer = 0f;
        // Always reset timer when a stack is consumed so recharge starts fresh
        rechargeTimer = 0f;
        RechargeProgress = 0f;

        Vector3 dashDir = inputDir.sqrMagnitude > 0.01f
            ? inputDir
            : new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).normalized;
        if (dashDir.sqrMagnitude < 0.01f)
            dashDir = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;

        Vector3 currentFlat = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float opposing = Mathf.Min(Vector3.Dot(currentFlat, dashDir), 0f);
        Vector3 corrected = currentFlat - dashDir * opposing;
        rb.linearVelocity = new Vector3(corrected.x, rb.linearVelocity.y, corrected.z);
        rb.AddForce(dashDir * dashForce, ForceMode.Impulse);

        PlayDashVFX();
        yield return null; // coroutine kept for future use (e.g. dash frames, invincibility)
    }

    void PlayDashVFX()
    {
        if (dashVFXPoint == null) return;
        if (dashVFXPrefab != null)
        {
            ParticleSystem fx = Instantiate(dashVFXPrefab, dashVFXPoint.position, dashVFXPoint.rotation);
            fx.Play();
            Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.5f);
        }
        else
        {
            ParticleSystem ex = dashVFXPoint.GetComponentInChildren<ParticleSystem>();
            if (ex != null) { ex.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ex.Play(); }
        }
    }

    void ApplyOrbitalForce(float direction)
    {
        if (orbitTarget == null) return;

        Vector3 toPlayer = transform.position - orbitTarget.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.001f) return;

        float   currentRadius = toPlayer.magnitude;
        Vector3 radialDir     = toPlayer.normalized;

        // ── Tangential force (slides the player around the circle) ─────────
        Vector3 tangent = direction > 0f
            ? new Vector3( toPlayer.z, 0f, -toPlayer.x).normalized
            : new Vector3(-toPlayer.z, 0f,  toPlayer.x).normalized;
        rb.AddForce(tangent * orbitForce, ForceMode.Force);

        // ── Radial correction (keeps the radius locked) ────────────────────
        // orbitRadialDrift == 0  → force pulls player exactly back to locked radius
        // orbitRadialDrift  > 0  → target radius grows  → player drifts outward
        // orbitRadialDrift  < 0  → target radius shrinks → player drifts inward
        if (lockedOrbitRadius > 0f)
        {
            float targetRadius = lockedOrbitRadius + orbitRadialDrift * Time.deltaTime;
            lockedOrbitRadius  = targetRadius;   // accumulate drift over time

            float radiusError  = currentRadius - targetRadius;   // +ve = too far, -ve = too close
            // Apply inward force proportional to how far off the target radius we are
            rb.AddForce(-radialDir * radiusError * orbitRadiusStiffness, ForceMode.Force);
        }
    }

    void TiltTowardVelocity()
    {
        Vector3 flat      = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float   speedFrac = Mathf.Clamp01(flat.magnitude / maxSpeed);
        Quaternion targetRot = flat.sqrMagnitude > 0.01f
            ? Quaternion.AngleAxis(maxTiltAngle * speedFrac, Vector3.Cross(Vector3.up, flat.normalized))
            : Quaternion.identity;
        Quaternion yOnly = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, yOnly * targetRot, Time.fixedDeltaTime * tiltSpeed);
    }
}
