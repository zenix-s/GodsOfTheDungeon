using Godot;

namespace GodsOfTheDungeon.Core.Data;

[GlobalClass]
public partial class EntityStats : Resource
{
    [Export] public int MaxHP { get; set; } = 100;
    [Export] public int CurrentHP { get; set; } = 100;
    [Export] public int Attack { get; set; } = 10;
    [Export] public int Defense { get; set; } = 5;
    [Export] public float Speed { get; set; } = 100f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float CriticalChance { get; set; } = 0.05f;

    [Export] public float CriticalMultiplier { get; set; } = 1.5f;

    [Export(PropertyHint.Range, "0,1,0.01")]
    public float KnockbackResistance { get; set; }

    [Export] public float InvincibilityDuration { get; set; } = 0.5f;

    public bool IsDead => CurrentHP <= 0;

    public void Heal(int amount)
    {
        CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
    }

    public void ResetToMax()
    {
        CurrentHP = MaxHP;
    }

    public EntityStats Clone()
    {
        return (EntityStats)Duplicate();
    }
}
