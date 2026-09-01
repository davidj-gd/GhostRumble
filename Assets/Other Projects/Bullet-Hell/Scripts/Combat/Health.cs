using UnityEngine;
using System;

/// <summary>
/// Generic health/damage component. Attach to Player and every Enemy type.
/// Projectiles and melee attacks call TakeDamage() on this — they don't
/// need to know if they hit a player or an enemy, just the team.
/// </summary>
public enum Team { Player, Enemy }

public class Health : MonoBehaviour
{
    [Header("Setup")]
    public Team team = Team.Enemy;
    public float maxHealth = 10f;

    [Header("Runtime (read-only, just for debugging)")]
    [SerializeField] private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    // Other scripts subscribe to these instead of us calling into them directly.
    // Keeps Health.cs decoupled from XP drops, UI, death VFX, etc.
    public event Action<float, float> OnHealthChanged; // (current, max)
    public event Action OnDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    public void SetMaxHealth(float newMax, bool refill = true)
    {
        maxHealth = newMax;
        if (refill) currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(currentHealth, 0f);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    private void Die()
    {
        if (IsDead) return;
        IsDead = true;
        OnDeath?.Invoke();
    }
}
