using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Component that detects incoming attacks and emits signals.
///     Active detector - monitors for AttackHitBoxComponent collisions.
///     Parent connects to HitReceived signal to handle damage.
/// </summary>
public partial class HurtBoxComponent : Area2D
{
    [Signal]
    public delegate void HitReceivedEventHandler(AttackData attackData, EntityStats attackerStats,
        Vector2 attackerPosition);

    public override void _Ready()
    {
        Monitoring = true; // Detecta AttackHitBox
        Monitorable = false; // No necesita ser detectado
        AreaEntered += OnAreaEntered;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (area is AttackHitBoxComponent hitBox && hitBox.IsActive)
        {
            EntityStats stats = hitBox.OwnerStats ?? new EntityStats();
            EmitSignal(SignalName.HitReceived, hitBox.AttackData, stats, hitBox.GlobalPosition);

            // Notify the hitbox that hit connected (for attacker feedback)
            hitBox.NotifyHitConnected(GetParent());
        }
    }

    #region Configuration Helpers

    /// <summary>
    ///     Configure collision layer and mask for this hurtbox.
    /// </summary>
    public void ConfigureCollision(uint layer, uint mask)
    {
        CollisionLayer = layer;
        CollisionMask = mask;
    }

    /// <summary>
    ///     Set the capsule shape size (assumes CollisionShape2D child with CapsuleShape2D).
    /// </summary>
    public void SetShapeSize(float radius, float height)
    {
        CollisionShape2D shape = GetNodeOrNull<CollisionShape2D>("CollisionShape2D");
        if (shape?.Shape is CapsuleShape2D capsule)
        {
            capsule.Radius = radius;
            capsule.Height = height;
        }
    }

    #endregion
}
