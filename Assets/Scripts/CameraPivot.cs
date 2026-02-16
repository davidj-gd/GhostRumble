using UnityEngine;

[ExecuteAlways]   // Runs in editor and play mode
public class OrbitCamera : MonoBehaviour
{
    public Transform target;

    void Update()
    {
        if (target == null) return;

        // Always face the target
        transform.LookAt(target.position);
    }
}
