using UnityEngine;

/// <summary>
/// Spawns one-shot muzzle flash and impact particle prefabs.
/// </summary>
public static class CombatEffects
{
    public static void PlayMuzzleFlash(ProjectileVFXConfig config, Vector3 position, Quaternion rotation)
    {
        SpawnOneShot(config?.muzzleFlashPrefab, position, rotation, config?.muzzleFlashLifetime ?? 1.5f);
    }

    public static void PlayImpact(ProjectileVFXConfig config, Vector3 position, Quaternion rotation)
    {
        SpawnOneShot(config?.impactEffectPrefab, position, rotation, config?.impactEffectLifetime ?? 2f);
    }

    private static void SpawnOneShot(GameObject prefab, Vector3 position, Quaternion rotation, float fallbackLifetime)
    {
        if (prefab == null) return;

        GameObject instance = Object.Instantiate(prefab, position, rotation);
        float lifetime = fallbackLifetime;

        ParticleSystem ps = instance.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = instance.GetComponentInChildren<ParticleSystem>();

        if (ps != null)
        {
            ps.Play(true);
            ParticleSystem.MainModule main = ps.main;
            lifetime = Mathf.Max(lifetime, main.duration + main.startLifetime.constantMax);
        }

        Object.Destroy(instance, lifetime);
    }
}
