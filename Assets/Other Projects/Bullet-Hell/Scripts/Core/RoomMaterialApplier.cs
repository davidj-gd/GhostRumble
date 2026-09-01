using UnityEngine;

/// <summary>
/// Live material/shader testing for generated room planes.
/// Assign new materials in Play Mode and call ApplyNow().
/// </summary>
public class RoomMaterialApplier : MonoBehaviour
{
    public RoomSurfaceSettings surfaceSettings;
    public Material floorMaterialOverride;
    public Material wallMaterialOverride;

    public void ApplyNow()
    {
        Material floor = floorMaterialOverride != null
            ? floorMaterialOverride
            : surfaceSettings != null ? surfaceSettings.floorMaterial : null;

        Material wall = wallMaterialOverride != null
            ? wallMaterialOverride
            : surfaceSettings != null ? surfaceSettings.wallMaterial : null;

        RoomSurfacePlane[] planes = FindObjectsByType<RoomSurfacePlane>(FindObjectsSortMode.None);
        foreach (RoomSurfacePlane plane in planes)
        {
            if (plane.surfaceType == RoomSurfacePlane.SurfaceType.Floor)
                plane.ApplyMaterial(floor);
            else
                plane.ApplyMaterial(wall);
        }
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            ApplyNow();
    }
}
