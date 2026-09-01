using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Holds the player's stats, XP, and leveling logic.
/// Movement uses force + speed caps — tune base values on the Player in the Inspector.
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [Header("Movement (force + speed cap)")]
    public float baseMoveForce = 16f;
    public float baseMoveSpeed = 6f;
    public float baseDashExtraForce = 12f;
    public float baseDashSpeedBonus = 4f;

    [Header("Combat")]
    public float baseDamage = 5f;
    public float baseAttacksPerSecond = 4f;
    public float baseProjectileSpeed = 12f;
    public float baseMaxHealth = 100f;

    [Header("Current Multipliers (1 = no change, modified by perks)")]
    public float moveSpeedMult = 1f;
    public float dashSpeedMult = 1f;
    public float damageMult = 1f;
    public float attackSpeedMult = 1f;
    public float maxHealthMult = 1f;
    public float projectileSpeedMult = 1f;

    [Header("Leveling")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNextLevel = 8f;
    public float xpGrowthPerLevel = 1.18f;

    public float MoveForce => baseMoveForce * moveSpeedMult;
    public float MaxSpeed => baseMoveSpeed * moveSpeedMult;
    public float DashExtraForce => baseDashExtraForce * dashSpeedMult;
    public float DashSpeedBonus => baseDashSpeedBonus * dashSpeedMult;
    public float MaxDashSpeed => MaxSpeed + DashSpeedBonus;
    public float Damage => baseDamage * damageMult;
    public float AttacksPerSecond => baseAttacksPerSecond * attackSpeedMult;
    public float ProjectileSpeed => baseProjectileSpeed * projectileSpeedMult;
    public float MaxHealth => baseMaxHealth * maxHealthMult;

    public event Action<int> OnLevelUp;
    public event Action<float, float> OnXPChanged;

    private Health health;

    private void Awake()
    {
        health = GetComponent<Health>();
    }

    private void Start()
    {
        if (health != null)
            health.SetMaxHealth(MaxHealth, refill: true);

        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    public void AddXP(float amount)
    {
        currentXP += amount;

        while (currentXP >= xpToNextLevel)
        {
            currentXP -= xpToNextLevel;
            LevelUp();
        }

        OnXPChanged?.Invoke(currentXP, xpToNextLevel);
    }

    private void LevelUp()
    {
        level++;
        xpToNextLevel *= xpGrowthPerLevel;

        if (health != null)
        {
            health.SetMaxHealth(MaxHealth, refill: false);
            health.Heal(MaxHealth * 0.25f);
        }

        OnLevelUp?.Invoke(level);
    }

    public void AddDashGlideSpeedMult(float delta)
    {
        dashSpeedMult += delta;
    }

    public void AddMoveSpeedMult(float delta) => moveSpeedMult += delta;
    public void AddDamageMult(float delta) => damageMult += delta;
    public void AddAttackSpeedMult(float delta) => attackSpeedMult += delta;
    public void AddProjectileSpeedMult(float delta) => projectileSpeedMult += delta;

    public void AddMaxHealthMult(float delta)
    {
        maxHealthMult += delta;
        if (health != null) health.SetMaxHealth(MaxHealth, refill: false);
    }
}
