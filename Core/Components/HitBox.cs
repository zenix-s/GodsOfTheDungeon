using Godot;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;

namespace GodsOfTheDungeon.Core.Components;

public partial class HitBox : Area2D
{
	[Signal]
	public delegate void HitConnectedEventHandler(Node target, int damage, bool wasCritical);

	private IGameEntity _owner;
	private Node _ownerNode;

	[Export] public AttackData AttackData { get; set; }
	[Export] public bool IsActive { get; set; }

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

		GD.PushWarning("HitBox: No IGameEntity owner found");
		return null;
	}

	private void OnAreaEntered(Area2D area)
	{
		if (!IsActive) return;

		if (area is HurtBox hurtBox)
		{
			IDamageable target = hurtBox.GetDamageable();
			if (target == null || target.IsInvincible) return;

			// Don't hit yourself
			if ((Node)target == _ownerNode) return;

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

	public void SetAttack(AttackData attackData)
	{
		AttackData = attackData;
	}
}
