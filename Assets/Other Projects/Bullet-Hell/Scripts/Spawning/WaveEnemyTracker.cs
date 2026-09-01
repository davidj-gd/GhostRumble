using UnityEngine;

/// <summary>
/// Notifies WaveManager when a wave-spawned enemy dies.
/// Added automatically by WaveManager — do not attach manually.
/// </summary>
public class WaveEnemyTracker : MonoBehaviour
{
    private WaveManager manager;
    private Health health;
    private bool reported;

    public void Bind(WaveManager waveManager, Health enemyHealth)
    {
        manager = waveManager;
        health = enemyHealth;
        health.OnDeath += ReportDeath;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.OnDeath -= ReportDeath;
    }

    private void ReportDeath()
    {
        if (reported) return;
        reported = true;
        manager?.NotifyEnemyDefeated();
    }
}
