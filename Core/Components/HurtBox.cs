using Godot;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

public partial class HurtBox : Area2D
{
    [Signal]
    public delegate void DamageReceivedEventHandler(int damage, bool wasCritical);

    private IDamageable _owner;

    public override void _Ready()
    {
        _owner = GetOwnerEntity();

        if (_owner == null)
            GD.PushError($"HurtBox: Owner must implement IDamageable. Node: {GetPath()}");

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

    public IDamageable GetDamageable()
    {
        return _owner;
    }
}
