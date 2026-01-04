using Godot;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
/// Component that receives damage and notifies its parent IDamageable.
/// This is the active receiver in the damage flow - it calls TakeDamage on its parent.
/// </summary>
public partial class HurtBoxComponent : Area2D
{
    [Signal]
    public delegate void DamageReceivedEventHandler(int damage, bool wasCritical);

    [Signal]
    public delegate void HitReceivedEventHandler();

    private IDamageable _owner;
    private Node _ownerNode;

    public override void _Ready()
    {
        _owner = GetOwnerEntity();
        _ownerNode = _owner as Node;

        if (_owner == null)
            GD.PushError($"HurtBoxComponent: Owner must implement IDamageable. Node: {GetPath()}");

        Monitorable = true;
        Monitoring = false;
    }

    private IDamageable GetOwnerEntity()
    {
        Node current = GetParent();
        while (current != null)
        {
            if (current is IDamageable damageable)
                return damageable;
            current = current.GetParent();
        }
        return null;
    }

    /// <summary>
    /// Called by HitBoxComponent when a hit connects.
    /// HurtBoxComponent actively calls TakeDamage on its parent IDamageable.
    /// </summary>
    public DamageResult NotifyHit(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (_owner == null)
        {
            GD.PushError("HurtBoxComponent: No owner to receive damage");
            return DamageResult.Blocked;
        }

        if (_owner.IsInvincible)
            return DamageResult.Blocked;

        // Emit signal before processing (allows for shields, damage reduction hooks)
        EmitSignal(SignalName.HitReceived);

        // Delegate damage handling to parent IDamageable
        DamageResult result = _owner.TakeDamage(attackData, attackerStats, attackerPosition);

        // Emit damage received signal after processing
        if (!result.WasBlocked)
            EmitSignal(SignalName.DamageReceived, result.FinalDamage, result.WasCritical);

        return result;
    }

    /// <summary>
    /// Returns the parent IDamageable for reference.
    /// </summary>
    public IDamageable GetDamageable()
    {
        return _owner;
    }

    /// <summary>
    /// Returns the owner node for self-hit prevention.
    /// </summary>
    public Node GetOwnerNode()
    {
        return _ownerNode;
    }
}
