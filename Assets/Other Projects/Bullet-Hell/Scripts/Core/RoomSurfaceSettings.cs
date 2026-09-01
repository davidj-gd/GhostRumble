using UnityEngine;

/// <summary>
/// Materials for 3D room planes. Swap these to test floor/wall shaders.
/// </summary>
[CreateAssetMenu(fileName = "RoomSurfaceSettings", menuName = "Bullet Hell/Room Surface Settings")]
public class RoomSurfaceSettings : ScriptableObject
{
    public Material floorMaterial;
    public Material wallMaterial;

    [Header("Fallback Colors (used when material is null)")]
    public Color floorColor = new Color(0.12f, 0.14f, 0.18f);
    public Color wallColor = new Color(0.22f, 0.24f, 0.3f);
}
