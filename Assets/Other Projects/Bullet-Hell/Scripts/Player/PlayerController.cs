using UnityEngine;

/// <summary>
/// Ice-physics movement with mouse aim. Attack sheet plays before each shot.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    private const float SpriteFacingOffset = -90f;

    [Header("References")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    public AttackAnimationData animationData;

    [Header("Ice Drag")]
    [Range(0f, 5f)] public float idleDrag = 1.2f;
    [Range(0f, 2f)] public float movingDrag = 0.08f;

    public bool IsDashing { get; private set; }

    private Rigidbody2D rb;
    private PlayerStats stats;
    private Camera cam;
    private UnitVisuals combatVisuals;
    private Transform aimPivot;

    private Vector2 moveInput;
    private float fireTimer;
    private Quaternion aimRotation = Quaternion.identity;
    private bool attackInProgress;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        stats = GetComponent<PlayerStats>();
        cam = Camera.main;
        rb.gravityScale = 0f;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        EnsureVisualSetup();
        SyncAnimationData();
    }

    private void Start()
    {
        if (GameplayTuning.Instance != null)
        {
            GameplayTuning.Instance.ApplyPlayerScale(transform);
            GameplayTuning.Instance.ApplyToUnitVisuals(combatVisuals);
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsRunEnded)
            return;

        ReadMovementInput();
        IsDashing = Input.GetKey(KeyCode.LeftShift);
        rb.linearDamping = moveInput.sqrMagnitude > 0.01f ? movingDrag : idleDrag;

        AimAtMouse();
        HandleShootInput();
    }

    private void FixedUpdate()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsRunEnded)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (moveInput.sqrMagnitude > 0.01f)
        {
            float force = IsDashing
                ? stats.MoveForce + stats.DashExtraForce
                : stats.MoveForce;
            rb.AddForce(moveInput * force, ForceMode2D.Force);
        }

        ApplySpeedCap();
    }

    private void EnsureVisualSetup()
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
        }

        aimPivot = combatVisuals.transform;

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

    private void SyncAnimationData()
    {
        if (combatVisuals != null && animationData != null)
            combatVisuals.Configure(animationData);
    }

    private void ReadMovementInput()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");
        moveInput = GetCameraAlignedInput(x, y);
    }

    private Vector2 GetCameraAlignedInput(float x, float y)
    {
        if (cam == null)
            return new Vector2(x, y).normalized;

        Vector2 camRight = cam.transform.right;
        Vector2 camUp = cam.transform.up;
        Vector2 world = camRight * x + camUp * y;

        if (world.sqrMagnitude < 0.001f)
            return Vector2.zero;

        return world.normalized;
    }

    private void AimAtMouse()
    {
        if (cam == null || aimPivot == null) return;

        Vector3 mouseWorld;
        if (cam.orthographic)
            mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        else
            mouseWorld = cam.ScreenToWorldPoint(new Vector3(
                Input.mousePosition.x,
                Input.mousePosition.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z)));
        mouseWorld.z = 0f;

        Vector2 dir = (Vector2)mouseWorld - rb.position;
        if (dir.sqrMagnitude < 0.001f) return;
        dir.Normalize();

        float aimAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        aimRotation = Quaternion.Euler(0f, 0f, aimAngle);
        aimPivot.rotation = Quaternion.Euler(0f, 0f, aimAngle + SpriteFacingOffset);
    }

    private void ApplySpeedCap()
    {
        Vector2 velocity = rb.linearVelocity;
        float speed = velocity.magnitude;
        if (speed < 0.01f) return;

        if (IsDashing)
        {
            float dashCap = stats.MaxDashSpeed;
            if (speed > dashCap)
                rb.linearVelocity = velocity.normalized * dashCap;
            return;
        }

        if (moveInput.sqrMagnitude > 0.01f && speed > stats.MaxSpeed)
            rb.linearVelocity = velocity.normalized * stats.MaxSpeed;
    }

    private void HandleShootInput()
    {
        fireTimer -= Time.deltaTime;
        if (!Input.GetMouseButton(0) || fireTimer > 0f || attackInProgress)
            return;

        BeginAttackShot();
    }

    private void BeginAttackShot()
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        SyncAnimationData();

        if (combatVisuals == null || animationData == null)
        {
            Shoot();
            fireTimer = 1f / Mathf.Max(stats.AttacksPerSecond, 0.01f);
            return;
        }

        attackInProgress = true;
        combatVisuals.PlayAttack(animationData, () =>
        {
            attackInProgress = false;
            Shoot();
            fireTimer = 1f / Mathf.Max(stats.AttacksPerSecond, 0.01f);
        });
    }

    private void Shoot()
    {
        Projectile prefabProjectile = projectilePrefab.GetComponent<Projectile>();
        if (prefabProjectile != null)
            CombatEffects.PlayMuzzleFlash(prefabProjectile.vfx, firePoint.position, aimRotation);

        GameObject bullet = Instantiate(projectilePrefab, firePoint.position, aimRotation);
        Projectile proj = bullet.GetComponent<Projectile>();
        if (proj != null)
            proj.Init(Team.Player, stats.Damage, stats.ProjectileSpeed);
    }
}
