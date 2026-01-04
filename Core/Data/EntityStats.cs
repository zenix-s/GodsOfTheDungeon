using Godot;

namespace GodsOfTheDungeon.Core.Data;

/// <summary>
///     Combat and movement stats for entities.
///     Health is now managed by HealthComponent.
/// </summary>
[GlobalClass]
public partial class EntityStats : Resource
{
    [Export] public int Attack { get; set; } = 10;
    [Export] public int Defense { get; set; } = 5;
    [Export] public float Speed { get; set; } = 100f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float CriticalChance { get; set; } = 0.05f;

    [Export] public float CriticalMultiplier { get; set; } = 1.5f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float KnockbackResistance { get; set; }

    public EntityStats Clone()
    {
        return (EntityStats)Duplicate();
    }
}
