using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Core.Interfaces;

public interface IGameEntity
{
    EntityStats Stats { get; }
    bool IsInvincible { get; }

    DamageResult TakeDamage(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition);
    void Die();
    void ApplyKnockback(Vector2 force);
}
