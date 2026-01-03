using Godot;
using GodsOfTheDungeon.Core.Enums;

namespace GodsOfTheDungeon.Core.Data;

[GlobalClass]
public partial class AttackData : Resource
{
    [Export] public string AttackName { get; set; } = "Basic Attack";
    [Export] public int BaseDamage { get; set; } = 10;
    [Export] public DamageType DamageType { get; set; } = DamageType.Physical;

    [Export] public float KnockbackForce { get; set; } = 200f;
    [Export] public Vector2 KnockbackDirection { get; set; } = Vector2.Right;
    [Export] public bool UseAttackerFacing { get; set; } = true;

    [Export] public bool CanCrit { get; set; } = true;
    [Export] public float StunDuration { get; set; }
    [Export] public float DamageMultiplier { get; set; } = 1.0f;
}
