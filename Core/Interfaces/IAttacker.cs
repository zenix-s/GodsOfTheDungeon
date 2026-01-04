using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Core.Interfaces;

/// <summary>
/// Interface for entities that can perform attacks.
/// </summary>
public interface IAttacker
{
    EntityStats Stats { get; }
    AttackData CurrentAttack { get; }
}
