using UnityEngine;

/// <summary>Put this on the projectile prefab so it destroys itself when it hits something.</summary>
public class ProjectileHit : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Destroy(gameObject);
    }

    void OnCollisionEnter(Collision other)
    {
        Destroy(gameObject);
    }
}
