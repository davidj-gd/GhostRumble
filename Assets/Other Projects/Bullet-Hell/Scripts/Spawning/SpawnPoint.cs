using UnityEngine;

/// <summary>
/// Empty marker placed in the scene where enemies can appear.
/// WaveManager reads these Transform positions when spawning.
///
/// Setup:
///  - Create an empty GameObject, add this script
///  - Place several around the room edges
///  - Assign them to WaveManager's Spawn Points array
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
