using UnityEngine;

/// <summary>
/// Orthographic camera on +Z looking toward the XY play plane at z=0.
/// Attach to Main Camera (root level, not parented to Player).
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    [Tooltip("Camera sits on +Z and looks toward the play plane.")]
    public Vector3 offset = new Vector3(0f, 0f, 10f);

    [Header("Zoom")]
    [Tooltip("Higher = zoomed out. Orthographic camera only.")]
    public float orthographicSize = 10f;

    private Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        ApplyViewSetup();
    }

    private void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position + offset;
    }

    private void OnValidate()
    {
        if (cam == null)
            cam = GetComponent<Camera>();
        ApplyViewSetup();
    }

    private void ApplyViewSetup()
    {
        transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        if (cam != null && cam.orthographic)
            cam.orthographicSize = orthographicSize;
    }
}
