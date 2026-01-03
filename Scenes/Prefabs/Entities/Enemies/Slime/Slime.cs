using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Entities;
using GodsOfTheDungeon.Core.Interfaces;

public partial class Slime : GameEntity, IEnemy
{
	private Timer _attackCooldownTimer;
	private bool _canAttack = true;
	private SlimeState _currentState = SlimeState.Idle;
	private Player _targetPlayer;
	private bool _isPlayerInRange;

	[Export] public float AttackCooldown = 1.5f;
	[Export] public float AttackRange = 20f;
	[Export] public float ChaseSpeed = 60f;
	[Export] public float PatrolSpeed = 30f;
	[Export] public AttackData AttackData { get; set; }

	public override void _Ready()
	{
		// Set slime-specific stats before base._Ready()
		Stats = new EntityStats
		{
			MaxHP = 20,
			CurrentHP = 20,
			Attack = 3,
			Defense = 1,
			Speed = PatrolSpeed,
			KnockbackResistance = 0.3f,
			InvincibilityDuration = 0.3f,
			CriticalChance = 0f
		};

		AttackData ??= new AttackData
		{
			AttackName = "Slime Bump",
			BaseDamage = 1,
			KnockbackForce = 100f,
			CanCrit = false
		};

		base._Ready();

		SetupAttackCooldown();
		SetupDetectionArea();
	}

	private void SetupAttackCooldown()
	{
		_attackCooldownTimer = new Timer();
		_attackCooldownTimer.OneShot = true;
		_attackCooldownTimer.WaitTime = AttackCooldown;
		_attackCooldownTimer.Timeout += () => _canAttack = true;
		AddChild(_attackCooldownTimer);
	}

	private void SetupDetectionArea()
	{
		var detectionArea = GetNodeOrNull<Area2D>("DetectionArea");
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
		if (Stats.IsDead) return;

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
				Sprite?.Play("idle");
				// Apply gravity only
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

		// Apply gravity
		if (!IsOnFloor()) Velocity += GetGravity() * delta;

		Sprite?.Play("idle");
	}

	private void FaceTarget(Vector2 targetPosition)
	{
		if (Sprite != null)
			Sprite.FlipH = targetPosition.X < GlobalPosition.X;
	}

	private void PerformAttack()
	{
		if (!_canAttack || _targetPlayer == null) return;

		_canAttack = false;
		_attackCooldownTimer.Start();

		// Deal damage if in range
		if (_targetPlayer is IGameEntity entity)
		{
			float distance = GlobalPosition.DistanceTo(_targetPlayer.GlobalPosition);
			if (distance <= AttackRange)
				entity.TakeDamage(AttackData, Stats, GlobalPosition);
		}
	}

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

	public override void Die()
	{
		base.Die();
		GameManager.Instance?.OnEnemyKilled(this);
		QueueFree();
	}

	private enum SlimeState
	{
		Idle,
		Chase,
		Attack
	}
}
