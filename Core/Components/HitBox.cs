using Godot;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Entities;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Attach to an Area2D that deals damage when overlapping with a HurtBox.
///     Parent must be a DamageableEntity (Player, Enemy, etc.)
/// </summary>
public partial class HitBox : Area2D
{
    [Signal]
    public delegate void HitConnectedEventHandler(Node target, int damage, bool wasCritical);

    private DamageableEntity _owner;

    [Export] public AttackData AttackData { get; set; }
    [Export] public bool IsActive { get; set; }

    public override void _Ready()
    {
        _owner = GetOwnerEntity();

        // HitBox monitors for HurtBoxes
        Monitoring = IsActive;
        Monitorable = false;

        AreaEntered += OnAreaEntered;
    }

    private DamageableEntity GetOwnerEntity()
    {
        Node current = GetParent();
        while (current != null)
        {
            if (current is DamageableEntity entity)
                return entity;
            current = current.GetParent();
        }

        GD.PushWarning("HitBox: No DamageableEntity owner found");
        return null;
    }

    private void OnAreaEntered(Area2D area)
    {
        if (!IsActive) return;

        if (area is HurtBox hurtBox)
        {
            IDamageable target = hurtBox.GetDamageableOwner();
            if (target == null || target.IsInvincible) return;

            // Don't hit yourself
            if (target == _owner) return;

            if (AttackData == null)
            {
                GD.PushError("HitBox: No AttackData assigned");
                return;
            }

            EntityStats stats = _owner?.Stats ?? new EntityStats();
            DamageResult result = target.TakeDamage(AttackData, stats, GlobalPosition);

            EmitSignal(SignalName.HitConnected, (Node)target, result.FinalDamage, result.WasCritical);
        }
    }

    public void SetActive(bool active)
    {
        IsActive = active;
        SetDeferred("monitoring", active);
    }

    /// <summary>
    ///     Switch to a different attack (for entities with multiple attacks)
    /// </summary>
    public void SetAttack(AttackData attackData)
    {
        AttackData = attackData;
    }
}
