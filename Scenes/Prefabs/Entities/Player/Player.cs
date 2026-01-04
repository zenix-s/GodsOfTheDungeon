using System.Collections.Generic;
using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

public partial class Player : CharacterBody2D, IGameEntity
{
    // Attack configs: animation name and duration per attack
    private readonly Dictionary<int, (string anim, float duration)> _attackConfigs = new()
    {
        { 1, ("attack_slash", 0.25f) },
        { 2, ("attack_thrust", 0.3f) },
        { 3, ("attack_heavy", 0.5f) }
    };

    private Timer _attackTimer;
    private AnimatedSprite2D _currentEffectSprite;
    private HitBoxComponent _currentHitBox;

    private Label _debugLabel;
    private HealthComponent _health;
    private HitBoxComponent _heavySwingHitBox;
    private HurtBoxComponent _hurtBox;
    private bool _isAttacking;
    private HitBoxComponent _slashHitBox;
    private AnimatedSprite2D _sprite;
    private HitBoxComponent _thrustHitBox;

    // Movement exports
    [Export] public float Acceleration = 1500.0f;
    [Export] public bool CanMove = true;
    [Export] public float FallGravityMultiplier = 2.5f;
    [Export] public float Friction = 1200.0f;
    [Export] public float JumpCutMultiplier = 0.5f;
    [Export] public float JumpVelocity = -400.0f;
    [Export] public float Speed = 300.0f;

    public bool IsFacingRight { get; private set; } = true;


    // Signal handler from HurtBoxComponent

    // IGameEntity implementation
    [Export] public EntityStats Stats { get; set; }

    private void OnHitReceived(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (_health.IsInvincible || _health.IsDead) return;

        DamageResult result = DamageCalculator.CalculateDamage(
            attackData,
            attackerStats,
            Stats,
            attackerPosition,
            GlobalPosition);

        _health.ApplyDamage(result.FinalDamage, result.WasCritical);

        // Apply knockback
        if (result.KnockbackApplied != Vector2.Zero)
            Velocity += result.KnockbackApplied;
    }

    public override void _Ready()
    {
        // Load and clone stats from GameManager
        Stats = GameManager.Instance?.GetPlayerStats()?.Clone() ?? new EntityStats();

        // Setup sprite
        _sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        // Setup components
        SetupHealthComponent();
        SetupHurtBoxComponent();
        SetupHitBoxComponents();

        _debugLabel = GetNode<Label>("DebugLabel");

        SetupAttackTimer();
    }

    private void SetupHealthComponent()
    {
        _health = GetNodeOrNull<HealthComponent>("HealthComponent");
        if (_health == null)
        {
            _health = new HealthComponent();
            AddChild(_health);
        }

        // Initialize health from GameManager
        PlayerData playerData = GameManager.Instance?.GetPlayerData();
        if (playerData != null)
            _health.Initialize(playerData.MaxHP, playerData.CurrentHP, playerData.InvincibilityDuration);

        // Connect health signals
        _health.DamageTaken += OnDamageTaken;
        _health.Died += OnDied;
        _health.InvincibilityStarted += OnInvincibilityStarted;
        _health.InvincibilityEnded += OnInvincibilityEnded;
        _health.HealthChanged += OnHealthChanged;
    }

    private void SetupHurtBoxComponent()
    {
        _hurtBox = GetNodeOrNull<HurtBoxComponent>("HurtBox");
        if (_hurtBox != null)
            _hurtBox.HitReceived += OnHitReceived;
    }

    private void SetupHitBoxComponents()
    {
        _slashHitBox = GetNodeOrNull<HitBoxComponent>("SlashHitBox");
        _thrustHitBox = GetNodeOrNull<HitBoxComponent>("ThrustHitBox");
        _heavySwingHitBox = GetNodeOrNull<HitBoxComponent>("HeavySwingHitBox");

        // Set owner stats for damage calculation
        _slashHitBox?.SetOwnerStats(Stats);
        _thrustHitBox?.SetOwnerStats(Stats);
        _heavySwingHitBox?.SetOwnerStats(Stats);
    }

    private void SetupAttackTimer()
    {
        _attackTimer = new Timer();
        _attackTimer.OneShot = true;
        _attackTimer.Timeout += OnAttackFinished;
        AddChild(_attackTimer);
    }

    public override void _Input(InputEvent @event)
    {
        if (!CanMove) return;

        if (@event.IsActionPressed("ui_accept") && IsOnFloor())
        {
            Vector2 velocity = Velocity;
            velocity.Y = JumpVelocity;
            Velocity = velocity;
        }

        if (@event.IsActionReleased("ui_accept") && Velocity.Y < 0)
        {
            Vector2 velocity = Velocity;
            velocity.Y *= JumpCutMultiplier;
            Velocity = velocity;
        }

        if (!_isAttacking)
        {
            if (@event.IsActionPressed("attack_1"))
                PerformAttack(1, _slashHitBox);
            else if (@event.IsActionPressed("attack_2"))
                PerformAttack(2, _thrustHitBox);
            else if (@event.IsActionPressed("attack_3"))
                PerformAttack(3, _heavySwingHitBox);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!CanMove) return;

        Vector2 velocity = Velocity;
        float deltaF = (float)delta;

        if (!IsOnFloor())
        {
            float gravityMultiplier = velocity.Y > 0 ? FallGravityMultiplier : 1.0f;
            velocity += GetGravity() * deltaF * gravityMultiplier;
        }

        float direction = GetInputDirection();
        if (direction != 0)
        {
            velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * deltaF);
            IsFacingRight = direction > 0;
        }
        else
        {
            velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * deltaF);
        }

        if (_sprite != null)
            _sprite.FlipH = !IsFacingRight;

        // Flip all hitbox positions based on facing direction
        float xDir = IsFacingRight ? 1 : -1;
        if (_slashHitBox != null)
            _slashHitBox.Position = new Vector2(20 * xDir, 0);
        if (_thrustHitBox != null)
            _thrustHitBox.Position = new Vector2(25 * xDir, 0);
        if (_heavySwingHitBox != null)
            _heavySwingHitBox.Position = new Vector2(18 * xDir, 0);

        if (!_isAttacking && _sprite != null)
        {
            if (IsOnFloor())
                _sprite.Play(direction == 0 ? "idle" : "run");
            else
                _sprite.Play("jump");
        }

        Velocity = velocity;
        _debugLabel.Text = $"HP: {_health.CurrentHP}/{_health.MaxHP}\nVel: {Velocity}";
        MoveAndSlide();
    }

    private float GetInputDirection()
    {
        float direction = 0;
        if (Input.IsActionPressed("move_left"))
            direction -= 1;
        if (Input.IsActionPressed("move_right"))
            direction += 1;
        return direction;
    }

    private void PerformAttack(int attackIndex, HitBoxComponent hitBox)
    {
        if (hitBox == null)
        {
            GD.PushWarning($"Player: HitBox for attack {attackIndex} not found");
            return;
        }

        _isAttacking = true;
        _currentHitBox = hitBox;

        (string anim, float duration) = _attackConfigs[attackIndex];

        _currentHitBox.SetActive(true);
        _attackTimer.WaitTime = duration;
        _attackTimer.Start();

        // Play player character animation
        if (_sprite != null && _sprite.SpriteFrames.HasAnimation(anim))
            _sprite.Play(anim);

        // Show and play hitbox effect sprite
        _currentEffectSprite = hitBox.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_currentEffectSprite != null)
        {
            _currentEffectSprite.Visible = true;
            _currentEffectSprite.Play();
        }
    }

    private void OnAttackFinished()
    {
        _isAttacking = false;
        _currentHitBox?.SetActive(false);
        _currentHitBox = null;

        // Hide the effect sprite
        if (_currentEffectSprite != null)
        {
            _currentEffectSprite.Stop();
            _currentEffectSprite.Visible = false;
            _currentEffectSprite = null;
        }
    }

    // Signal handlers from HealthComponent
    private void OnDamageTaken(int damage, bool wasCritical)
    {
        PlayHitEffect();
    }

    private void OnDied()
    {
        CanMove = false;
        GameManager.Instance?.OnPlayerDied();
    }

    private void OnInvincibilityStarted()
    {
        StartInvincibilityVisual();
    }

    private void OnInvincibilityEnded()
    {
        StopInvincibilityVisual();
    }

    private void OnHealthChanged(int currentHP, int maxHP)
    {
        // Could update UI here if needed
    }

    public override void _ExitTree()
    {
        // Unsubscribe from HealthComponent signals
        if (_health != null)
        {
            _health.DamageTaken -= OnDamageTaken;
            _health.Died -= OnDied;
            _health.InvincibilityStarted -= OnInvincibilityStarted;
            _health.InvincibilityEnded -= OnInvincibilityEnded;
            _health.HealthChanged -= OnHealthChanged;
        }

        // Unsubscribe from HurtBox signal
        if (_hurtBox != null)
            _hurtBox.HitReceived -= OnHitReceived;

        // Unsubscribe from Timer
        if (_attackTimer != null)
            _attackTimer.Timeout -= OnAttackFinished;
    }

    private void StartInvincibilityVisual()
    {
        if (_sprite != null)
        {
            Tween tween = CreateTween();
            tween.SetLoops((int)(_health.InvincibilityDuration / 0.1f));
            tween.TweenProperty(_sprite, "modulate:a", 0.5f, 0.05f);
            tween.TweenProperty(_sprite, "modulate:a", 1.0f, 0.05f);
        }
    }

    private void StopInvincibilityVisual()
    {
        if (_sprite != null)
            _sprite.Modulate = Colors.White;
    }

    private void PlayHitEffect()
    {
        if (_sprite != null)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(_sprite, "modulate", new Color(1, 0.3f, 0.3f), 0.05f);
            tween.TweenProperty(_sprite, "modulate", Colors.White, 0.1f);
        }
    }

    private void _OnCollectionAreaEntered(Node2D body)
    {
        if (body is ICollectible collectible)
            collectible.Collect(this);
    }
}
