using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Core.Interfaces;

public interface IDamageable
{
    bool IsInvincible { get; }
    DamageResult TakeDamage(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition);
}
