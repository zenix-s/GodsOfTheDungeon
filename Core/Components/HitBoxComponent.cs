using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Component that represents an attack hitbox.
///     Passive component - detected by HurtBoxComponent, not the detector.
///     Parent sets OwnerStats for damage calculation.
/// </summary>
public partial class HitBoxComponent : Area2D
{
    [Signal]
    public delegate void HitConnectedEventHandler(Node target);

    [Export] public AttackData AttackData { get; set; }
    [Export] public bool IsActive { get; set; }

    public EntityStats OwnerStats { get; private set; }

    public override void _Ready()
    {
        Monitoring = false; // No detecta
        Monitorable = true; // Es detectado por HurtBox
    }

    /// <summary>
    ///     Parent calls this to set their stats for damage calculation.
    /// </summary>
    public void SetOwnerStats(EntityStats stats)
    {
        OwnerStats = stats;
    }

    /// <summary>
    ///     Activate or deactivate this hitbox.
    /// </summary>
    public void SetActive(bool active)
    {
        IsActive = active;
        SetDeferred("monitorable", active);
    }

    /// <summary>
    ///     Set the attack data at runtime.
    /// </summary>
    public void SetAttack(AttackData attackData)
    {
        AttackData = attackData;
    }

    /// <summary>
    ///     Called by HurtBoxComponent when hit connects (for feedback to attacker).
    /// </summary>
    public void NotifyHitConnected(Node target)
    {
        EmitSignal(SignalName.HitConnected, target);
    }

    #region Configuration Helpers

    /// <summary>
    ///     Configure collision layer and mask for this hitbox.
    /// </summary>
    public void ConfigureCollision(uint layer, uint mask)
    {
        CollisionLayer = layer;
        CollisionMask = mask;
    }

    /// <summary>
    ///     Set the rectangle shape size (assumes CollisionShape2D child with RectangleShape2D).
    /// </summary>
    public void SetShapeSize(Vector2 size)
    {
        CollisionShape2D shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (shape?.Shape is RectangleShape2D rect)
            rect.Size = size;
    }

    #endregion
}
