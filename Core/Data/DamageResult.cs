using Godot;

namespace GodsOfTheDungeon.Core.Data;

// public struct DamageResult
// {
//     public int FinalDamage { get; set; }
//     public bool WasCritical { get; set; }
//     public bool WasBlocked { get; set; }
//     public bool KilledTarget { get; set; }
//     public Vector2 KnockbackApplied { get; set; }
//
//     public static DamageResult Blocked => new()
//     {
//         FinalDamage = 0,
//         WasBlocked = true
//     };
// }

public readonly record struct DamageResult(
    int FinalDamage = 0,
    bool WasCritical = false,
    Vector2 KnockbackApplied = default
);
