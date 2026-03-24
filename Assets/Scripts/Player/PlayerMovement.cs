using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    Rigidbody rb;

    [Header("Move settings")]
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float linearDrag = 3f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 18f;
    [SerializeField] float dashCooldown = 0.7f;
    [SerializeField] float dashControlLockout = 0.12f;
    [Tooltip("How fast dash momentum blends to walk speed (with input) or zero (no input). Higher = snappier, lower = longer slide.")]
    [SerializeField] float dashMomentumBlendSpeed = 48f;

    bool dashQueued;
    float nextDashAllowedFixedTime = -1f;
    float dashMoveLockoutEnd;

    /// <summary>Total dash cooldown duration (seconds). Same as inspector <see cref="dashCooldown"/>.</summary>
    public float DashCooldownDuration => dashCooldown;

    /// <summary>Seconds until dash can be used again. Zero when ready.</summary>
    public float DashCooldownRemaining => Mathf.Max(0f, nextDashAllowedFixedTime - Time.fixedTime);

    /// <summary>0 right after dash, 1 when cooldown finished (for fill UI).</summary>
    public float DashCooldownNormalized01 => dashCooldown <= 0f ? 1f : 1f - Mathf.Clamp01(DashCooldownRemaining / dashCooldown);

    public bool CanDash => Time.fixedTime >= nextDashAllowedFixedTime;

    [Header("Aim Settings")]
    [SerializeField] float rotationSpeed = 100f;
    private Vector2 aimValue;

    [Header("Lean")]
    [SerializeField] float maxLeanPitchDegrees = 6f;
    [SerializeField] float maxLeanRollDegrees = 8f;
    [SerializeField] float leanSmoothSpeed = 12f;

    float leanPitch;
    float leanRoll;

    Transform lookAtTarget;

    [SerializeField] CinemachineCamera camera;
    [SerializeField] Camera cam;

    Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = linearDrag;

        if (cam == null)
            cam = Camera.main;

        if (lookAtTarget == null)
        {
            GameObject p2 = GameObject.FindGameObjectWithTag("P2");
            if (p2 != null)
                lookAtTarget = p2.transform;
        }
    }

    private void Update()
    {
        HandleRotation();
    }

    private void FixedUpdate()
    {
        if (dashQueued && Time.fixedTime >= nextDashAllowedFixedTime)
        {
            dashQueued = false;
            if (TryStartDash())
                nextDashAllowedFixedTime = Time.fixedTime + dashCooldown;
        }

        HandleMovement(moveInput);
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnDash(InputAction.CallbackContext context)
    {
        if (!context.performed)
            return;
        dashQueued = true;
    }

    Vector3 GetCameraPlanarMoveDirection(Vector2 inputVector)
    {
        if (camera == null || inputVector.sqrMagnitude < 0.001f)
            return Vector3.zero;

        Vector3 camForward = camera.transform.forward;
        Vector3 camRight = camera.transform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        return (camForward * inputVector.y + camRight * inputVector.x).normalized;
    }

    bool TryStartDash()
    {
        if (camera == null)
            return false;

        Vector3 dir = GetCameraPlanarMoveDirection(moveInput);
        if (dir.sqrMagnitude < 0.001f)
        {
            Quaternion leanQuat = Quaternion.Euler(leanPitch, 0f, leanRoll);
            dir = transform.rotation * Quaternion.Inverse(leanQuat) * Vector3.forward;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f)
                dir = Vector3.forward;
            else
                dir.Normalize();
        }

        Vector3 v = rb.linearVelocity;
        rb.linearVelocity = new Vector3(dir.x * dashSpeed, v.y, dir.z * dashSpeed);
        dashMoveLockoutEnd = Time.fixedTime + dashControlLockout;
        return true;
    }

    private void HandleMovement(Vector2 inputVector)
    {
        if (Time.fixedTime < dashMoveLockoutEnd)
            return;

        if (camera == null)
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            return;
        }

        Vector3 moveDirection = GetCameraPlanarMoveDirection(inputVector);
        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float walkSpeedSq = moveSpeed * moveSpeed;

        if (moveDirection.sqrMagnitude < 0.001f)
        {
            if (horizontalVel.sqrMagnitude <= walkSpeedSq)
                return;

            Vector3 slowed = Vector3.MoveTowards(horizontalVel, Vector3.zero, dashMomentumBlendSpeed * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector3(slowed.x, rb.linearVelocity.y, slowed.z);
            return;
        }

        Vector3 targetHorizontal = moveDirection * moveSpeed;
        if (horizontalVel.sqrMagnitude <= walkSpeedSq + 0.25f)
        {
            rb.linearVelocity = new Vector3(targetHorizontal.x, rb.linearVelocity.y, targetHorizontal.z);
            return;
        }

        Vector3 blended = Vector3.MoveTowards(horizontalVel, targetHorizontal, dashMomentumBlendSpeed * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector3(blended.x, rb.linearVelocity.y, blended.z);
    }

    private void HandleRotation()
    {
        if (cam == null) return;

        Vector3 screenPosition = new Vector3(aimValue.x, aimValue.y, 0f);

        Ray raycast = cam.ScreenPointToRay(screenPosition);
        float playerHeight = transform.position.y;
        Plane plane = new Plane(Vector3.up, new Vector3(0f, playerHeight, 0f));

        if (!plane.Raycast(raycast, out float distance)) return;

        Vector3 hitPoint = raycast.GetPoint(distance);
        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;

        Quaternion targetRotation = transform.rotation;
        if (direction.sqrMagnitude > 0.001f)
        {
            direction.Normalize();
            targetRotation = Quaternion.LookRotation(direction);
        }

        float maxDegrees = rotationSpeed * Time.deltaTime;
        Quaternion leanQuat = Quaternion.Euler(leanPitch, 0f, leanRoll);
        Quaternion currentAim = transform.rotation * Quaternion.Inverse(leanQuat);
        Quaternion nextAim = Quaternion.RotateTowards(currentAim, targetRotation, maxDegrees);

        Vector3 forward = nextAim * Vector3.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        else
            forward.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

        float targetPitch = 0f;
        float targetRoll = 0f;
        if (moveInput.sqrMagnitude > 0.001f && camera != null)
        {
            Vector3 camForward = camera.transform.forward;
            Vector3 camRight = camera.transform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            Vector3 moveDir = (camForward * moveInput.y + camRight * moveInput.x).normalized;
            float forwardAmount = Vector3.Dot(moveDir, forward);
            float strafeAmount = Vector3.Dot(moveDir, right);
            targetPitch = maxLeanPitchDegrees * forwardAmount;
            targetRoll = -maxLeanRollDegrees * strafeAmount;
        }

        float t = leanSmoothSpeed * Time.deltaTime;
        leanPitch = Mathf.Lerp(leanPitch, targetPitch, t);
        leanRoll = Mathf.Lerp(leanRoll, targetRoll, t);

        transform.rotation = nextAim * Quaternion.Euler(leanPitch, 0f, leanRoll);
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        aimValue = context.ReadValue<Vector2>();
    }
}
