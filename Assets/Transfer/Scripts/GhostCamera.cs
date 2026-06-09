using UnityEngine;

/// <summary>
/// GhostCamera — Keeps two players always in view.
///
/// The camera positions itself above the midpoint between the two targets.
/// Distance is driven by how far apart the players are, clamped between
/// a min and max zoom. The angle and offset you set in the editor is
/// preserved as the "look direction" — only distance changes at runtime.
///
/// SETUP:
///   1. Position and rotate the camera in the Scene view exactly how you
///      want it to look (angle, tilt, side offset — anything).
///   2. Assign targetA (Player 1) and targetB (Player 2 / enemy).
///   3. Press Play — the camera holds your angle and slides in/out
///      along that same axis based on player separation.
/// </summary>
public class GhostCamera : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetA;   // Player 1
    public Transform targetB;   // Player 2 / enemy

    [Header("Zoom")]
    [Tooltip("Closest the camera will get (used when players are on top of each other).")]
    public float minDistance = 10f;

    [Tooltip("Furthest the camera will pull back (used when players are at max separation).")]
    public float maxDistance = 35f;

    [Tooltip("Extra padding so players are never right at the screen edge.")]
    public float zoomPadding = 4f;

    [Tooltip("How quickly the camera zooms in/out.")]
    public float zoomSpeed = 4f;

    [Header("Follow Smoothing")]
    [Tooltip("How quickly the camera moves to the midpoint.")]
    public float followSpeed = 6f;

    // ── private ────────────────────────────────────────────────────────────
    Vector3    camOffset;       // direction + distance snapshot from editor position
    Vector3    smoothVel;
    float      smoothDist;
    float      initialDistance;

    void Start()
    {
        if (targetA == null || targetB == null)
        {
            Debug.LogError("[GhostCamera] Assign both targets in the Inspector!");
            return;
        }

        Vector3 midpoint = GetMidpoint();

        // Capture the offset direction from the initial midpoint so we
        // always travel along the same axis the designer set in the editor.
        camOffset       = transform.position - midpoint;
        initialDistance = camOffset.magnitude;
        smoothDist      = initialDistance;
    }

    void LateUpdate()
    {
        if (targetA == null || targetB == null) return;

        Vector3 midpoint   = GetMidpoint();
        float   separation = Vector3.Distance(targetA.position, targetB.position);

        // Map separation to a target distance along the camera's offset axis.
        // When separation = 0  → minDistance
        // When separation is large → approaches maxDistance
        // zoomPadding adds a small buffer so players aren't clipped to edges.
        float targetDist = Mathf.Clamp(separation + zoomPadding, minDistance, maxDistance);

        // Smooth the distance change independently of position follow
        smoothDist = Mathf.Lerp(smoothDist, targetDist, Time.deltaTime * zoomSpeed);

        // Desired position: midpoint + offset direction scaled to smoothed distance
        Vector3 offsetDir    = camOffset.normalized;
        Vector3 desiredPos   = midpoint + offsetDir * smoothDist;

        transform.position = Vector3.SmoothDamp(
            transform.position, desiredPos,
            ref smoothVel, 1f / followSpeed);

        // Always look at the midpoint between the two players
        transform.LookAt(midpoint);
    }

    Vector3 GetMidpoint()
    {
        return (targetA.position + targetB.position) * 0.5f;
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (targetA == null || targetB == null) return;
        Vector3 mid = GetMidpoint();
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, mid);
        Gizmos.DrawWireSphere(mid, 0.4f);
        Gizmos.DrawLine(targetA.position, targetB.position);
    }
#endif
}
