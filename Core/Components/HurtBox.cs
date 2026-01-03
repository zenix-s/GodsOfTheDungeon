using Godot;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Attach to an Area2D that represents where an entity can receive damage.
///     The parent node must implement IDamageable.
/// </summary>
public partial class HurtBox : Area2D
{
    [Signal]
    public delegate void DamageReceivedEventHandler(int damage, bool wasCritical);

    private IDamageable _owner;

    public override void _Ready()
    {
        _owner = GetOwnerDamageable();

        if (_owner == null) GD.PushError($"HurtBox: Owner must implement IDamageable. Node: {GetPath()}");

        // HurtBox should be detectable but not detect others
        Monitorable = true;
        Monitoring = false;
    }

    private IDamageable GetOwnerDamageable()
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

    public IDamageable GetDamageableOwner()
    {
        return _owner;
    }
}
