using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Core.Systems;

public static class DamageCalculator
{
    /// <summary>
    ///     Calculate final damage based on the formula:
    ///     FinalDamage = max(1, (AttackPower × BaseDamage × Multiplier) - Defense) × CritMultiplier
    /// </summary>
    public static DamageResult CalculateDamage(
        AttackData attack,
        EntityStats attackerStats,
        EntityStats targetStats,
        Vector2 attackerPosition,
        Vector2 targetPosition)
    {
        bool wasCritical = false;
        // DamageResult result = new();

        // Base damage calculation
        float rawDamage = CalculateAttackBaseDamage(attack, attackerStats);

        // Apply defense
        float afterDefense = rawDamage - targetStats.Defense;

        // Minimum 1 damage
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(afterDefense));

        // Critical hit check
        if (attack.CanCrit && attackerStats.CriticalChance > GD.Randf())
        {
            finalDamage = Mathf.RoundToInt(finalDamage * attackerStats.CriticalMultiplier);
            wasCritical = true;
        }

        // result.FinalDamage = finalDamage;

        // Calculate knockback direction
        Vector2 knockbackDir = attack.UseAttackerFacing
            ? (targetPosition - attackerPosition).Normalized()
            : attack.KnockbackDirection.Normalized();

        // Handle case where positions are the same
        if (knockbackDir == Vector2.Zero) knockbackDir = Vector2.Right;

        float actualKnockback = attack.KnockbackForce * (1f - targetStats.KnockbackResistance);
        // result.KnockbackApplied = knockbackDir * actualKnockback;

        DamageResult result = new(
            finalDamage,
            wasCritical,
            knockbackDir * actualKnockback);

        return result;
    }

    private static float CalculateAttackBaseDamage(AttackData attack, EntityStats attackerStats)
    {
        return attackerStats.Attack * attack.BaseDamage * attack.DamageMultiplier;
    }
}
