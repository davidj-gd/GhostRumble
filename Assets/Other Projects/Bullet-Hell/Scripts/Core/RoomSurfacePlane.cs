using UnityEngine;

/// <summary>
/// Marks a generated room plane so materials can be swapped at runtime.
/// </summary>
public class RoomSurfacePlane : MonoBehaviour
{
    public enum SurfaceType
    {
        Floor,
        Wall
    }

    public SurfaceType surfaceType;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void ApplyMaterial(Material material)
    {
        if (material == null || meshRenderer == null) return;
        meshRenderer.sharedMaterial = material;
    }
}
