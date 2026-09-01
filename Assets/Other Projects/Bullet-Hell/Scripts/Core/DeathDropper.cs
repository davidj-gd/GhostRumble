using UnityEngine;

/// <summary>
/// Spawns an XP orb when the entity's Health component fires OnDeath.
/// Attach to every enemy prefab alongside Health.
///
/// Setup:
///  - Health component on the same GameObject
///  - Assign XP Orb prefab to xpOrbPrefab
/// </summary>
[RequireComponent(typeof(Health))]
public class DeathDropper : MonoBehaviour
{
    public GameObject xpOrbPrefab;
    public float xpAmount = 1f;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        health.OnDeath += DropXP;
    }

    private void OnDisable()
    {
        health.OnDeath -= DropXP;
    }

    private void DropXP()
    {
        if (xpOrbPrefab == null) return;

        GameObject orb = Instantiate(xpOrbPrefab, transform.position, Quaternion.identity);
        XPOrb xp = orb.GetComponent<XPOrb>();
        if (xp != null)
            xp.Init(xpAmount);
    }
}
