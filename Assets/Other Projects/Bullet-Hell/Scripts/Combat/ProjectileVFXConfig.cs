using UnityEngine;

/// <summary>
/// Assign imported particle prefabs here. Leave empty until art is ready.
/// </summary>
[CreateAssetMenu(fileName = "ProjectileVFX", menuName = "Bullet Hell/Projectile VFX")]
public class ProjectileVFXConfig : ScriptableObject
{
    [Header("Prefabs (ParticleSystem root)")]
    public GameObject muzzleFlashPrefab;
    public GameObject impactEffectPrefab;

    [Header("Projectile Body")]
    public bool hideSpriteRenderer = true;
    [Tooltip("Optional override. If null, uses ParticleSystem on the projectile prefab.")]
    public GameObject travelParticlePrefab;

    [Header("Timing")]
    public float muzzleFlashLifetime = 1.5f;
    public float impactEffectLifetime = 2f;
}
