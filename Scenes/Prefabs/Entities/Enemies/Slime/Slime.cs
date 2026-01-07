using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.StateMachine;

public partial class Slime : CharacterBody2D, IGameEntity, IEnemy
{
    private Timer _attackCooldownTimer;

    // System references
    public AliveEntityComponentManager AliveComponents { get; private set; }
    public StateMachine StateMachine { get; private set; }

    // Direct component references
    public HealthComponent HealthComponent { get; private set; }
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

    // IGameEntity implementation
    [Export] public EntityStats Stats { get; set; }

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
        AliveComponents = GetNode<AliveEntityComponentManager>("ComponentManager");
        StateMachine = GetNode<StateMachine>("StateMachine");

        // Get direct component references
        HealthComponent = AliveComponents.Health;
        AttackHitBox = GetNodeOrNull<AttackHitBoxComponent>("AttackHitBox");

        // Configure attack hitbox
        if (AttackHitBox != null)
        {
            AttackHitBox.SetOwnerStats(Stats);
            AttackHitBox.AttackData = AttackData;
            AttackHitBox.SetActive(false);
        }

        // Connect to AliveEntityComponentManager signals
        AliveComponents.DamageTaken += OnDamageTaken;
        AliveComponents.Died += OnDied;

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
        if (body is Player player) OnPlayerDetected(player);
    }

    private void OnDetectionBodyExited(Node2D body)
    {
        if (body is Player) OnPlayerLost();
    }

    private void OnDamageTaken(int damage, bool wasCritical)
    {
        PlayHitEffect();
        StateMachine.TransitionTo("Hurt");
    }

    private void OnDied()
    {
        QueueFree();
    }

    public override void _ExitTree()
    {
        if (AliveComponents != null)
        {
            AliveComponents.DamageTaken -= OnDamageTaken;
            AliveComponents.Died -= OnDied;
        }

        if (_attackCooldownTimer != null) _attackCooldownTimer.Timeout -= OnAttackCooldownComplete;

        Area2D detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (detectionArea != null)
        {
            detectionArea.BodyEntered -= OnDetectionBodyEntered;
            detectionArea.BodyExited -= OnDetectionBodyExited;
        }
    }

    private void PlayHitEffect()
    {
        AnimatedSprite2D sprite = AliveComponents?.Animation?.Sprite;
        if (sprite != null)
        {
            Tween tween = CreateTween();
            tween.TweenProperty(sprite, "modulate", new Color(1, 0.3f, 0.3f), 0.05f);
            tween.TweenProperty(sprite, "modulate", Colors.White, 0.1f);
        }
    }
}
