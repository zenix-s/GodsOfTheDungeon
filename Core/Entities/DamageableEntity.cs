using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

namespace GodsOfTheDungeon.Core.Entities;

/// <summary>
///     Base class for any entity that can take damage.
///     Extend this for Player, Enemies, Destructibles, etc.
/// </summary>
public abstract partial class DamageableEntity : CharacterBody2D, IDamageable
{
    [Signal]
    public delegate void DamagedEventHandler(int damage, bool wasCritical);

    [Signal]
    public delegate void DiedEventHandler();

    [Signal]
    public delegate void HealthChangedEventHandler(int currentHP, int maxHP);

    private Timer _invincibilityTimer;

    protected AnimatedSprite2D Sprite;
    [Export] public EntityStats Stats { get; set; }

    public bool IsInvincible { get; protected set; }

    public virtual DamageResult TakeDamage(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (IsInvincible)
            return DamageResult.Blocked;

        DamageResult result = DamageCalculator.CalculateDamage(
            attackData,
            attackerStats,
            Stats,
            attackerPosition,
            GlobalPosition);

        // Apply damage
        Stats.CurrentHP -= result.FinalDamage;

        // Emit signals
        EmitSignal(SignalName.Damaged, result.FinalDamage, result.WasCritical);
        EmitSignal(SignalName.HealthChanged, Stats.CurrentHP, Stats.MaxHP);

        // Notify GameManager
        GameManager.Instance?.OnEntityDamaged(this, result.FinalDamage, result.WasCritical);

        // Apply knockback
        if (result.KnockbackApplied != Vector2.Zero) ApplyKnockback(result.KnockbackApplied);

        // Start i-frames
        StartInvincibility();

        // Visual feedback
        PlayHitEffect();

        // Check death
        if (Stats.IsDead)
        {
            result.KilledTarget = true;
            Die();
        }

        return result;
    }

    public virtual void ApplyKnockback(Vector2 force)
    {
        Velocity += force;
    }

    public virtual void Die()
    {
        EmitSignal(SignalName.Died);
        GameManager.Instance?.OnEntityDied(this);
    }

    public override void _Ready()
    {
        // Clone stats so each instance has its own copy
        if (Stats != null)
            Stats = Stats.Clone();
        else
            Stats = new EntityStats();

        SetupInvincibilityTimer();
        SetupVisuals();

        EmitSignal(SignalName.HealthChanged, Stats.CurrentHP, Stats.MaxHP);
    }

    private void SetupInvincibilityTimer()
    {
        _invincibilityTimer = new Timer();
        _invincibilityTimer.OneShot = true;
        _invincibilityTimer.Timeout += OnInvincibilityEnded;
        AddChild(_invincibilityTimer);
    }

    protected virtual void SetupVisuals()
    {
        Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
    }

    protected virtual void StartInvincibility()
    {
        IsInvincible = true;
        _invincibilityTimer.WaitTime = Stats.InvincibilityDuration;
        _invincibilityTimer.Start();
        StartInvincibilityVisual();
    }

    private void OnInvincibilityEnded()
    {
        IsInvincible = false;
        StopInvincibilityVisual();
    }

    protected virtual void StartInvincibilityVisual()
    {
        // Flash effect during i-frames
        if (Sprite != null)
        {
            Tween tween = CreateTween();
            tween.SetLoops((int)(Stats.InvincibilityDuration / 0.1f));
            tween.TweenProperty(Sprite, "modulate:a", 0.5f, 0.05f);
            tween.TweenProperty(Sprite, "modulate:a", 1.0f, 0.05f);
        }
    }

    protected virtual void PlayHitEffect()
    {
        if (Sprite != null)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(Sprite, "modulate", new Color(1, 0.3f, 0.3f), 0.05f);
            tween.TweenProperty(Sprite, "modulate", Colors.White, 0.1f);
        }
    }

    protected virtual void StopInvincibilityVisual()
    {
        if (Sprite != null) Sprite.Modulate = Colors.White;
    }
}
