using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

public partial class Slime : CharacterBody2D, IGameEntity, IEnemy
{
    private Timer _attackCooldownTimer;
    private AttackHitBoxComponent _attackHitBox;
    private bool _canAttack = true;
    private SlimeState _currentState = SlimeState.Idle;
    private HealthComponent _health;
    private HurtBoxComponent _hurtBox;
    private bool _isPlayerInRange;
    private AnimatedSprite2D _sprite;
    private Player _targetPlayer;

    [Export] public float AttackCooldown = 1.5f;
    [Export] public float AttackRange = 20f;
    [Export] public float ChaseSpeed = 60f;
    [Export] public float PatrolSpeed = 30f;
    [Export] public AttackData AttackData { get; set; }

    // IEnemy implementation
    public void OnPlayerDetected(Player player)
    {
        _targetPlayer = player;
        _isPlayerInRange = true;
        _currentState = SlimeState.Chase;
    }

    public void OnPlayerLost()
    {
        _isPlayerInRange = false;
        _currentState = SlimeState.Idle;
    }

    // IGameEntity implementation
    [Export] public EntityStats Stats { get; set; }

    private void OnHitReceived(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (_health.IsDead) return;

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

        // Setup HealthComponent
        _health = GetNode<HealthComponent>("HealthComponent");
        _health.DamageTaken += OnDamageTaken;
        _health.Died += OnDied;

        _sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

        SetupHurtBoxComponent();
        SetupHitBoxComponent();
        SetupAttackCooldown();
        SetupDetectionArea();
    }

    private void SetupHurtBoxComponent()
    {
        _hurtBox = GetNodeOrNull<HurtBoxComponent>("HurtBox");
        if (_hurtBox != null)
            _hurtBox.HitReceived += OnHitReceived;
    }

    private void SetupHitBoxComponent()
    {
        _attackHitBox = GetNodeOrNull<AttackHitBoxComponent>("AttackHitBox");
        if (_attackHitBox != null)
        {
            _attackHitBox.SetOwnerStats(Stats);
            _attackHitBox.AttackData = AttackData;
        }
    }

    private void SetupAttackCooldown()
    {
        _attackCooldownTimer = GetNode<Timer>("AttackCooldownTimer");
        _attackCooldownTimer.WaitTime = AttackCooldown;
        _attackCooldownTimer.Timeout += OnAttackCooldownComplete;
    }

    private void OnAttackCooldownComplete()
    {
        _canAttack = true;
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

    private void OnDetectionBodyEntered(Node2D body)
    {
        if (body is Player player)
            OnPlayerDetected(player);
    }

    private void OnDetectionBodyExited(Node2D body)
    {
        if (body is Player)
            OnPlayerLost();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_health.IsDead) return;

        UpdateState();
        ProcessState((float)delta);

        MoveAndSlide();
    }

    private void UpdateState()
    {
        if (!_isPlayerInRange || _targetPlayer == null)
        {
            _currentState = SlimeState.Idle;
            return;
        }

        float distanceToPlayer = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);

        if (distanceToPlayer <= AttackRange && _canAttack)
            _currentState = SlimeState.Attack;
        else
            _currentState = SlimeState.Chase;
    }

    private void ProcessState(float delta)
    {
        switch (_currentState)
        {
            case SlimeState.Idle:
                _sprite?.Play("idle");
                if (!IsOnFloor())
                    Velocity += GetGravity() * delta;
                else
                    Velocity = new Vector2(0, Velocity.Y);
                break;

            case SlimeState.Chase:
                ChasePlayer(delta);
                break;

            case SlimeState.Attack:
                PerformAttack();
                break;
        }
    }

    private void ChasePlayer(float delta)
    {
        if (_targetPlayer == null) return;

        FaceTarget(_targetPlayer.GlobalPosition);

        Vector2 direction = (_targetPlayer.GlobalPosition - GlobalPosition).Normalized();
        Velocity = new Vector2(direction.X * ChaseSpeed, Velocity.Y);

        if (!IsOnFloor())
            Velocity += GetGravity() * delta;

        _sprite?.Play("idle");
    }

    private void FaceTarget(Vector2 targetPosition)
    {
        if (_sprite != null)
            _sprite.FlipH = targetPosition.X < GlobalPosition.X;
    }

    private void PerformAttack()
    {
        if (!_canAttack || _targetPlayer == null) return;

        _canAttack = false;
        _attackCooldownTimer.Start();

        // Activate hitbox briefly - Player's HurtBox will detect collision
        if (_attackHitBox != null)
        {
            _attackHitBox.SetActive(true);
            // Deactivate after a short time (attack duration)
            GetTree().CreateTimer(0.2f).Timeout += () => _attackHitBox?.SetActive(false);
        }
    }

    // Signal handlers from HealthComponent
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
        // Unsubscribe from HealthComponent signals
        if (_health != null)
        {
            _health.DamageTaken -= OnDamageTaken;
            _health.Died -= OnDied;
        }

        // Unsubscribe from HurtBox signal
        if (_hurtBox != null)
            _hurtBox.HitReceived -= OnHitReceived;

        // Unsubscribe from Timer
        if (_attackCooldownTimer != null)
            _attackCooldownTimer.Timeout -= OnAttackCooldownComplete;

        // Unsubscribe from detection area
        Area2D detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
        if (detectionArea != null)
        {
            detectionArea.BodyEntered -= OnDetectionBodyEntered;
            detectionArea.BodyExited -= OnDetectionBodyExited;
        }
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

    private enum SlimeState
    {
        Idle,
        Chase,
        Attack
    }
}
