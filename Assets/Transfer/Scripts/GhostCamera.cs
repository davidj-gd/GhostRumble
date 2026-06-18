using UnityEngine;

/// <summary>
/// GhostCamera — Keeps one or two players in view.
///
/// If only targetA is assigned, the camera simply follows targetA at
/// minDistance (no zoom logic, nothing to compare separation against).
/// If both targets are assigned, it behaves as before: tracks the midpoint
/// and zooms based on how far apart the two players are.
///
/// SETUP:
///   1. Position and rotate the camera in the Scene view exactly how you
///      want it to look (angle, tilt, side offset — anything).
///   2. Assign targetA (Player 1). Assign targetB only if/when Player 2 exists.
///   3. Press Play — the camera holds your angle and slides in/out
///      along that same axis based on player separation (if targetB is set).
/// </summary>
public class GhostCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetA;   // Player 1 — required
    public Transform targetB;   // Player 2 / enemy — optional, can be left empty

    [Header("Zoom")]
    [Tooltip("Closest the camera will get (used when players are close, or when only targetA exists).")]
    public float minDistance = 10f;

    [Tooltip("Furthest the camera will pull back (used when players are at max separation).")]
    public float maxDistance = 35f;

    [Tooltip("Extra padding so players are never right at the screen edge.")]
    public float zoomPadding = 4f;

    [Tooltip("How quickly the camera zooms in/out.")]
    public float zoomSpeed = 4f;

    [Header("Follow Smoothing")]
    [Tooltip("How quickly the camera moves to the target.")]
    public float followSpeed = 6f;

    // ── private ────────────────────────────────────────────────────────────
    Vector3    camOffset;       // direction snapshot from editor position
    Vector3    smoothVel;
    float      smoothDist;

    void Start()
    {
        if (targetA == null)
        {
            Debug.LogError("[GhostCamera] Assign targetA (Player 1) in the Inspector!");
            return;
        }

        Vector3 focusPoint = GetFocusPoint();

        // Capture the offset direction from the initial focus point so we
        // always travel along the same axis the designer set in the editor.
        camOffset  = transform.position - focusPoint;
        smoothDist = HasSecondTarget() ? camOffset.magnitude : minDistance;
    }

    void LateUpdate()
    {
        if (targetA == null) return;

        Vector3 focusPoint = GetFocusPoint();
        float   targetDist;

        if (HasSecondTarget())
        {
            // Two players: zoom based on how far apart they are
            float separation = Vector3.Distance(targetA.position, targetB.position);
            targetDist = Mathf.Clamp(separation + zoomPadding, minDistance, maxDistance);
        }
        else
        {
            // Only one player: just hold at minDistance, no zoom logic needed
            targetDist = minDistance;
        }

        smoothDist = Mathf.Lerp(smoothDist, targetDist, Time.deltaTime * zoomSpeed);

        Vector3 offsetDir  = camOffset.normalized;
        Vector3 desiredPos = focusPoint + offsetDir * smoothDist;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref smoothVel, 1f / followSpeed);

        transform.LookAt(focusPoint);
    }

    bool HasSecondTarget() => targetB != null;

    Vector3 GetFocusPoint()
    {
        // Single target: just follow targetA directly.
        // Two targets: follow the midpoint between them.
        return HasSecondTarget()
            ? (targetA.position + targetB.position) * 0.5f
            : targetA.position;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (targetA == null) return;
        Vector3 focus = GetFocusPoint();
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, focus);
        Gizmos.DrawWireSphere(focus, 0.4f);
        if (HasSecondTarget())
            Gizmos.DrawLine(targetA.position, targetB.position);
    }
#endif
}
