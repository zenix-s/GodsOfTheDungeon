using System;
using Godot;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Component that detects collisions and initiates damage on HurtBoxComponent.
///     Calls HurtBoxComponent.NotifyHit() which then delegates to the parent IDamageable.
/// </summary>
public partial class HitBoxComponent : Area2D
{
    private IGameEntity _owner;
    private Node _ownerNode;

    [Export] public AttackData AttackData { get; set; }
    [Export] public bool IsActive { get; set; }

    public event Action<Node, int, bool> HitConnected; // (target, damage, wasCritical)

    public override void _Ready()
    {
        _ownerNode = GetOwnerNode();
        _owner = _ownerNode as IGameEntity;

        Monitoring = IsActive;
        Monitorable = false;

        AreaEntered += OnAreaEntered;
    }

    private Node GetOwnerNode()
    {
        Node current = GetParent();
        while (current != null)
        {
            if (current is IGameEntity)
                return current;
            current = current.GetParent();
        }

        GD.PushWarning("HitBoxComponent: No IGameEntity owner found");
        return null;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (!IsActive) return;

        if (area is HurtBoxComponent hurtBoxComponent) ProcessHit(hurtBoxComponent);
    }

    private void ProcessHit(HurtBoxComponent hurtBox)
    {
        // Don't hit yourself
        if (hurtBox.GetOwnerNode() == _ownerNode) return;

        if (AttackData == null)
        {
            GD.PushError("HitBoxComponent: No AttackData assigned");
            return;
        }

        EntityStats stats = _owner?.Stats ?? new EntityStats();

        // NEW FLOW: Call NotifyHit on HurtBoxComponent
        // HurtBoxComponent is responsible for calling TakeDamage on its parent
        DamageResult result = hurtBox.NotifyHit(AttackData, stats, GlobalPosition);

        if (!result.WasBlocked)
        {
            Node targetNode = hurtBox.GetOwnerNode();
            HitConnected?.Invoke(targetNode, result.FinalDamage, result.WasCritical);
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        SetDeferred("monitoring", active);
    }

    public void SetAttack(AttackData attackData)
    {
        AttackData = attackData;
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
