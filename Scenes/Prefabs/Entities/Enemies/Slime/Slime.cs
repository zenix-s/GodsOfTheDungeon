using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

public partial class Slime : CharacterBody2D, IGameEntity, IEnemy
{
    // System references
    public ComponentManager Components { get; private set; }
    public GodsOfTheDungeon.Core.StateMachine.StateMachine StateMachine { get; private set; }

    // Direct component references
    public HealthComponent HealthComponent { get; private set; }
    public HurtBoxComponent HurtBoxComponent { get; private set; }
    public AttackHitBoxComponent AttackHitBox { get; private set; }

    // State machine accessible properties
    public Player TargetPlayer { get; private set; }
    public bool IsPlayerInRange { get; private set; }
    public bool CanAttack { get; private set; } = true;

    [Export] public float AttackCooldown { get; set; } = 1.5f;
    [Export] public float AttackRange { get; set; } = 20f;
    [Export] public float ChaseSpeed { get; set; } = 60f;
    [Export] public float PatrolSpeed { get; set; } = 30f;
    [Export] public AttackData AttackData { get; set; }

    // IGameEntity implementation
    [Export] public EntityStats Stats { get; set; }

    private Timer _attackCooldownTimer;

    public override void _Ready()
    {
        Stats = new EntityStats
        {
            Attack = 3,
            Defense = 1,
            Speed = PatrolSpeed,
            KnockbackResistance = 0.3f,
            CriticalChance = 0f
        };

        AttackData ??= new AttackData
        {
            AttackName = "Slime Bump",
            BaseDamage = 1,
            KnockbackForce = 100f,
            CanCrit = false
        };

        // Get systems
        Components = GetNode<ComponentManager>("ComponentManager");
        StateMachine = GetNode<GodsOfTheDungeon.Core.StateMachine.StateMachine>("StateMachine");

        // Get existing components
        HealthComponent = GetNode<HealthComponent>("HealthComponent");
        HurtBoxComponent = GetNodeOrNull<HurtBoxComponent>("HurtBox");
        AttackHitBox = GetNodeOrNull<AttackHitBoxComponent>("AttackHitBox");

        // Register external components with manager
        Components.RegisterExternalComponents(HealthComponent, HurtBoxComponent, AttackHitBox);

        // Configure attack hitbox
        if (AttackHitBox != null)
        {
            AttackHitBox.SetOwnerStats(Stats);
            AttackHitBox.AttackData = AttackData;
            AttackHitBox.SetActive(false);
        }

        // Setup animation component reference to sprite
        var animationComponent = Components.Animation;
        if (animationComponent != null && animationComponent.Sprite == null)
        {
            animationComponent.Sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        }

        // Setup health signals
        HealthComponent.DamageTaken += OnDamageTaken;
        HealthComponent.Died += OnDied;

        // Setup hurtbox
        if (HurtBoxComponent != null)
        {
            HurtBoxComponent.HitReceived += OnHitReceived;
        }

        // Setup detection area
        SetupDetectionArea();

        // Setup attack cooldown timer
        SetupAttackCooldown();

        // Initialize state machine LAST
        StateMachine.Initialize(this);
    }

    private void SetupDetectionArea()
    {
        Area2D detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (detectionArea != null)
        {
            detectionArea.BodyEntered += OnDetectionBodyEntered;
            detectionArea.BodyExited += OnDetectionBodyExited;
        }
    }

    private void SetupAttackCooldown()
    {
        _attackCooldownTimer = GetNodeOrNull<Timer>("AttackCooldownTimer");
        if (_attackCooldownTimer == null)
        {
            _attackCooldownTimer = new Timer();
            _attackCooldownTimer.OneShot = true;
            AddChild(_attackCooldownTimer);
        }
        _attackCooldownTimer.WaitTime = AttackCooldown;
        _attackCooldownTimer.Timeout += OnAttackCooldownComplete;
    }

    // IEnemy implementation
    public void OnPlayerDetected(Player player)
    {
        TargetPlayer = player;
        IsPlayerInRange = true;
        StateMachine.TransitionTo("Chase");
    }

    public void OnPlayerLost()
    {
        IsPlayerInRange = false;
        StateMachine.TransitionTo("Idle");
    }

    // Called by SlimeAttackState
    public void StartAttackCooldown()
    {
        CanAttack = false;
        _attackCooldownTimer.Start();
    }

    private void OnAttackCooldownComplete()
    {
        CanAttack = true;
    }

    private void OnDetectionBodyEntered(Node2D body)
    {
        if (body is Player player)
        {
            OnPlayerDetected(player);
        }
    }

    private void OnDetectionBodyExited(Node2D body)
    {
        if (body is Player)
        {
            OnPlayerLost();
        }
    }

    private void OnHitReceived(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (HealthComponent.IsDead) return;

        DamageResult result = DamageCalculator.CalculateDamage(
            attackData,
            attackerStats,
            Stats,
            attackerPosition,
            GlobalPosition);

        HealthComponent.ApplyDamage(result.FinalDamage, result.WasCritical);
        Components.Movement.ApplyKnockback(result.KnockbackApplied);
        StateMachine.TransitionTo("Hurt");
    }

    private void OnDamageTaken(int damage, bool wasCritical)
    {
        PlayHitEffect();
    }

    private void OnDied()
    {
        QueueFree();
    }

    public override void _ExitTree()
    {
        if (HealthComponent != null)
        {
            HealthComponent.DamageTaken -= OnDamageTaken;
            HealthComponent.Died -= OnDied;
        }

        if (HurtBoxComponent != null)
        {
            HurtBoxComponent.HitReceived -= OnHitReceived;
        }

        if (_attackCooldownTimer != null)
        {
            _attackCooldownTimer.Timeout -= OnAttackCooldownComplete;
        }

        Area2D detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (detectionArea != null)
        {
            detectionArea.BodyEntered -= OnDetectionBodyEntered;
            detectionArea.BodyExited -= OnDetectionBodyExited;
        }
    }

    private void PlayHitEffect()
    {
        var sprite = Components?.Animation?.Sprite;
        if (sprite != null)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(sprite, "modulate", new Color(1, 0.3f, 0.3f), 0.05f);
            tween.TweenProperty(sprite, "modulate", Colors.White, 0.1f);
        }
    }
}
