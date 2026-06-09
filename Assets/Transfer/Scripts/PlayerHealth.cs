using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// PlayerHealth — attach to Player 1 and Player 2.
/// Receives damage from GhostProjectile via TakeDamage().
/// Broadcasts events so the UI can react without tight coupling.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [Header("Stats")]
    public int maxHP = 25;

    [Header("Events")]
    public UnityEvent<int, int> onHealthChanged;  // (currentHP, maxHP)
    public UnityEvent           onDeath;

    // ── public read ────────────────────────────────────────────────────────
    public int  CurrentHP  { get; private set; }
    public bool IsDead     { get; private set; }

    void Awake() => CurrentHP = maxHP;

    /// <summary>Called by GhostProjectile on collision.</summary>
    public void TakeDamage(int amount = 1)
    {
        if (IsDead) return;

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        onHealthChanged?.Invoke(CurrentHP, maxHP);

        if (CurrentHP <= 0)
        {
            IsDead = true;
            onDeath?.Invoke();
        }
    }
}
