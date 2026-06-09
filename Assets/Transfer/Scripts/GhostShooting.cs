using UnityEngine;

/// <summary>
/// GhostShooting — Mouse aim (always active, even during Q/E orbit), left-click fire.
/// Rotation is fully independent of movement — GhostMovement no longer touches Y rotation.
/// </summary>
public class GhostShooting : MonoBehaviour
{
    [Header("References")]
    public GameObject     ballPrefab;
    public Transform      spawnPoint;
    public Camera         gameCamera;

    [Header("Muzzle Flash")]
    public ParticleSystem muzzleFlashVFX;

    [Header("Projectile Settings")]
    public float projectileSpeed = 22f;
    public float fireRate        = 0.3f;

    // ── private ────────────────────────────────────────────────────────────
    float    nextFireTime;
    Collider myCollider;

    void Awake()
    {
        if (gameCamera == null) gameCamera = Camera.main;
        myCollider = GetComponent<Collider>();
    }

    void Update()
    {
        RotateTowardMouse();

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            Fire();
            nextFireTime = Time.time + fireRate;
        }
    }

    void RotateTowardMouse()
    {
        if (gameCamera == null) return;

        // Plane at the player's Y so the ray always lands correctly
        Plane aimPlane = new Plane(Vector3.up, transform.position);
        Ray   ray      = gameCamera.ScreenPointToRay(Input.mousePosition);

        if (aimPlane.Raycast(ray, out float enter))
        {
            Vector3 dir = ray.GetPoint(enter) - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
            {
                float yAngle = Quaternion.LookRotation(dir).eulerAngles.y;
                // Only write Y rotation; X/Z tilt comes from GhostMovement
                transform.rotation = Quaternion.Euler(
                    transform.eulerAngles.x,
                    yAngle,
                    transform.eulerAngles.z);
            }
        }
    }

    void Fire()
    {
        if (ballPrefab == null) { Debug.LogError("[GhostShooting] Ball prefab not assigned!"); return; }

        Transform origin = spawnPoint != null ? spawnPoint : transform;

        // ── Muzzle flash ───────────────────────────────────────────────────
        if (muzzleFlashVFX != null)
        {
            ParticleSystem flash = Instantiate(muzzleFlashVFX, origin.position, origin.rotation);
            float d = flash.main.duration + flash.main.startLifetime.constantMax;
            Destroy(flash.gameObject, d + 0.1f);
        }

        // ── Spawn projectile ───────────────────────────────────────────────
        // Fire direction: purely horizontal (X/Z only) — no Y component ever
        Vector3 fireDir = transform.forward;
        fireDir.y = 0f;
        fireDir.Normalize();

        // Spawn at spawnPoint but force Y to match the player so it never
        // starts above or below the flat arena plane
        Vector3 spawnPos = new Vector3(origin.position.x, transform.position.y, origin.position.z);

        GameObject ball = Instantiate(ballPrefab, spawnPos, Quaternion.LookRotation(fireDir));

        Rigidbody ballRb = ball.GetComponent<Rigidbody>();
        if (ballRb == null) ballRb = ball.AddComponent<Rigidbody>();

        ballRb.useGravity             = false;
        ballRb.interpolation          = RigidbodyInterpolation.Interpolate;
        ballRb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        // Velocity is strictly on X/Z — zero Y guaranteed
        ballRb.linearVelocity = new Vector3(fireDir.x, 0f, fireDir.z) * projectileSpeed;

        // Freeze Y position and all rotation on the ball so physics can't
        // accidentally push it up/down and cause it to miss colliders
        ballRb.constraints = RigidbodyConstraints.FreezePositionY
                           | RigidbodyConstraints.FreezeRotationX
                           | RigidbodyConstraints.FreezeRotationZ;

        if (ball.GetComponent<Collider>() == null)
        {
            SphereCollider sc = ball.AddComponent<SphereCollider>();
            sc.radius = 0.25f;
        }

        if (myCollider != null)
            Physics.IgnoreCollision(ball.GetComponent<Collider>(), myCollider, true);

        if (ball.GetComponent<GhostProjectile>() == null)
            ball.AddComponent<GhostProjectile>();
    }
}
