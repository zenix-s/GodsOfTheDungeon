using Godot;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Attach to an Area2D that represents where an entity can receive damage.
///     The parent node must implement IGameEntity.
/// </summary>
public partial class HurtBox : Area2D
{
    [Signal]
    public delegate void DamageReceivedEventHandler(int damage, bool wasCritical);

    private IGameEntity _owner;

    public override void _Ready()
    {
        _owner = GetOwnerEntity();

        if (_owner == null) GD.PushError($"HurtBox: Owner must implement IGameEntity. Node: {GetPath()}");

        // HurtBox should be detectable but not detect others
        Monitorable = true;
        Monitoring = false;
    }

    private IGameEntity GetOwnerEntity()
    {
        Node current = GetParent();
        while (current != null)
        {
            if (current is IGameEntity entity)
                return entity;
            current = current.GetParent();
        }

        return null;
    }

    public IGameEntity GetGameEntity()
    {
        return _owner;
    }
}
