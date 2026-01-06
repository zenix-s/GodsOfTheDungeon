using Godot;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
/// Active component manager for alive entities.
/// Handles damage flow, signal wiring, and combat logic centrally.
/// Parent entity must implement IGameEntity.
/// </summary>
public partial class AliveEntityComponentManager : Node
{
    #region Signals

    [Signal]
    public delegate void DamageTakenEventHandler(int damage, bool wasCritical);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void HealedEventHandler(int amount);

    [Signal]
    public delegate void InvincibilityStartedEventHandler();

    [Signal]
    public delegate void InvincibilityEndedEventHandler();

    #endregion

    #region Component References (Export for editor assignment)

    [Export] public HurtBoxComponent HurtBox { get; set; }
    [Export] public HealthComponent Health { get; set; }
    [Export] public MovementComponent Movement { get; set; }
    [Export] public AnimationComponent Animation { get; set; }

    #endregion

    #region Runtime State

    private IGameEntity _owner;
    private Node2D _ownerNode;

    #endregion

    public override void _Ready()
    {
        _owner = GetParent() as IGameEntity;
        _ownerNode = GetParent() as Node2D;

        if (_owner == null)
        {
            GD.PushError($"AliveEntityComponentManager: Parent must implement IGameEntity");
            return;
        }

        ValidateComponents();
        WireSignals();
    }

    private void ValidateComponents()
    {
        if (HurtBox == null)
            GD.PushWarning("AliveEntityComponentManager: HurtBox not assigned");
        if (Health == null)
            GD.PushError("AliveEntityComponentManager: Health is required");
        if (Movement == null)
            GD.PushWarning("AliveEntityComponentManager: Movement not assigned (knockback disabled)");
    }

    private void WireSignals()
    {
        if (HurtBox != null)
            HurtBox.HitReceived += OnHitReceived;

        if (Health != null)
        {
            Health.DamageTaken += OnHealthDamageTaken;
            Health.Died += OnHealthDied;
            Health.Healed += OnHealthHealed;
            Health.InvincibilityStarted += OnHealthInvincibilityStarted;
            Health.InvincibilityEnded += OnHealthInvincibilityEnded;
        }
    }

    #region Internal Damage Flow

    private void OnHitReceived(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (Health == null || Health.IsDead || Health.IsInvincible)
            return;

        DamageResult result = DamageCalculator.CalculateDamage(
            attackData,
            attackerStats,
            _owner.Stats,
            attackerPosition,
            _ownerNode.GlobalPosition);

        bool wasApplied = Health.ApplyDamage(result.FinalDamage, result.WasCritical);

        if (wasApplied && Movement != null)
        {
            Movement.ApplyKnockback(result.KnockbackApplied);
        }
    }

    private void OnHealthDamageTaken(int damage, bool wasCritical)
    {
        EmitSignal(SignalName.DamageTaken, damage, wasCritical);
    }

    private void OnHealthDied()
    {
        EmitSignal(SignalName.Died);
    }

    private void OnHealthHealed(int amount)
    {
        EmitSignal(SignalName.Healed, amount);
    }

    private void OnHealthInvincibilityStarted()
    {
        EmitSignal(SignalName.InvincibilityStarted);
    }

    private void OnHealthInvincibilityEnded()
    {
        EmitSignal(SignalName.InvincibilityEnded);
    }

    #endregion

    #region Cleanup

    public override void _ExitTree()
    {
        if (HurtBox != null)
            HurtBox.HitReceived -= OnHitReceived;

        if (Health != null)
        {
            Health.DamageTaken -= OnHealthDamageTaken;
            Health.Died -= OnHealthDied;
            Health.Healed -= OnHealthHealed;
            Health.InvincibilityStarted -= OnHealthInvincibilityStarted;
            Health.InvincibilityEnded -= OnHealthInvincibilityEnded;
        }
    }

    #endregion
}
