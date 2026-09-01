using UnityEngine;

/// <summary>
/// Applies room-based stat scaling when an enemy is spawned.
/// Added at runtime by WaveManager — do not attach manually.
/// </summary>
public class EnemyRoomScaler : MonoBehaviour
{
    public void Apply(int room, float healthScalePerRoom, float speedScalePerRoom, float damageScalePerRoom)
    {
        if (room <= 1) return;

        float tiers = room - 1;
        float healthMult = 1f + tiers * healthScalePerRoom;
        float speedMult = 1f + tiers * speedScalePerRoom;
        float damageMult = 1f + tiers * damageScalePerRoom;

        Health health = GetComponent<Health>();
        if (health != null)
            health.SetMaxHealth(health.maxHealth * healthMult, refill: true);

        EnemyBase enemy = GetComponent<EnemyBase>();
        if (enemy != null)
        {
            enemy.moveSpeed *= speedMult;
            enemy.contactDamage *= damageMult;
        }

        EnemyRanged ranged = GetComponent<EnemyRanged>();
        if (ranged != null)
            ranged.projectileDamage *= damageMult;
    }
}
