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
        DamageResult result = new();

        // Base damage calculation
        float rawDamage = attackerStats.Attack * attack.BaseDamage * attack.DamageMultiplier;

        // Apply defense
        float afterDefense = rawDamage - targetStats.Defense;

        // Minimum 1 damage
        int finalDamage = Mathf.Max(1, Mathf.RoundToInt(afterDefense));

        // Critical hit check
        if (attack.CanCrit && GD.Randf() < attackerStats.CriticalChance)
        {
            finalDamage = Mathf.RoundToInt(finalDamage * attackerStats.CriticalMultiplier);
            result.WasCritical = true;
        }

        result.FinalDamage = finalDamage;

        // Calculate knockback direction
        Vector2 knockbackDir = attack.UseAttackerFacing
            ? (targetPosition - attackerPosition).Normalized()
            : attack.KnockbackDirection.Normalized();

        // Handle case where positions are the same
        if (knockbackDir == Vector2.Zero) knockbackDir = Vector2.Right;

        float actualKnockback = attack.KnockbackForce * (1f - targetStats.KnockbackResistance);
        result.KnockbackApplied = knockbackDir * actualKnockback;

        return result;
    }
}
