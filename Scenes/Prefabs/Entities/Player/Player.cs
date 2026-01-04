using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

public partial class Player : CharacterBody2D, IGameEntity, IDamageable, IAttacker, IMovable
{
	private HitBoxComponent _attackHitBox;
	private Timer _attackTimer;
	private AnimatedSprite2D _sprite;
	private HealthComponent _health;

	private Label _debugLabel;
	private bool _isAttacking;

	// Movement exports
	[Export] public float Acceleration = 1500.0f;
	[Export] public bool CanMove = true;
	[Export] public float FallGravityMultiplier = 2.5f;
	[Export] public float Friction = 1200.0f;
	[Export] public float JumpCutMultiplier = 0.5f;
	[Export] public float JumpVelocity = -400.0f;
	[Export] public float Speed = 300.0f;

	// Combat exports
	[Export] public AttackData Attack1Data { get; set; }
	[Export] public AttackData Attack2Data { get; set; }
	[Export] public AttackData Attack3Data { get; set; }
	[Export] public float AttackDuration = 0.3f;

	// IGameEntity implementation
	[Export] public EntityStats Stats { get; set; }

	// IDamageable implementation - delegate to HealthComponent
	public bool IsInvincible => _health?.IsInvincible ?? false;

	public bool IsFacingRight { get; private set; } = true;

	// IAttacker implementation
	public AttackData CurrentAttack { get; private set; }

	// IMovable implementation
	float IMovable.Speed => Speed;
	bool IMovable.CanMove { get => CanMove; set => CanMove = value; }

	public void Move(Vector2 direction, float delta)
	{
		Vector2 velocity = Velocity;
		if (direction != Vector2.Zero)
		{
			velocity.X = Mathf.MoveToward(velocity.X, direction.X * Speed, Acceleration * delta);
			IsFacingRight = direction.X > 0;
		}
		else
		{
			velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * delta);
		}
		Velocity = velocity;
	}

	public override void _Ready()
	{
		// Load and clone stats from GameManager
		Stats = GameManager.Instance?.GetPlayerStats()?.Clone() ?? new EntityStats();

		// Setup sprite
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Setup HealthComponent
		_health = GetNodeOrNull<HealthComponent>("HealthComponent");
		if (_health == null)
		{
			_health = new HealthComponent();
			AddChild(_health);
		}

		// Initialize health from GameManager
		var playerData = GameManager.Instance?.GetPlayerData();
		if (playerData != null)
			_health.Initialize(playerData.MaxHP, playerData.CurrentHP, playerData.InvincibilityDuration);

		// Connect health signals
		_health.DamageTaken += OnDamageTaken;
		_health.Died += OnDied;
		_health.InvincibilityStarted += OnInvincibilityStarted;
		_health.InvincibilityEnded += OnInvincibilityEnded;
		_health.HealthChanged += OnHealthChanged;

		_debugLabel = GetNode<Label>("DebugLabel");
		_attackHitBox = GetNodeOrNull<HitBoxComponent>("AttackHitBox");

		SetupAttackTimer();
		LoadDefaultAttacks();
	}

	private void LoadDefaultAttacks()
	{
		Attack1Data ??= GD.Load<AttackData>("res://Scenes/Prefabs/Entities/Player/Attacks/Slash.tres");
		Attack2Data ??= GD.Load<AttackData>("res://Scenes/Prefabs/Entities/Player/Attacks/Thrust.tres");
		Attack3Data ??= GD.Load<AttackData>("res://Scenes/Prefabs/Entities/Player/Attacks/HeavySwing.tres");
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
				PerformAttack(Attack1Data);
			else if (@event.IsActionPressed("attack_2"))
				PerformAttack(Attack2Data);
			else if (@event.IsActionPressed("attack_3"))
				PerformAttack(Attack3Data);
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

		if (_attackHitBox != null)
			_attackHitBox.Position = new Vector2(IsFacingRight ? 20 : -20, 0);

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

	private void PerformAttack(AttackData attackData)
	{
		if (_attackHitBox == null)
		{
			GD.PushWarning("Player: No AttackHitBox found");
			return;
		}

		_isAttacking = true;
		CurrentAttack = attackData;

		_attackHitBox.SetAttack(attackData);
		_attackHitBox.SetActive(true);

		_attackTimer.WaitTime = AttackDuration;
		_attackTimer.Start();

		if (_sprite != null && _sprite.SpriteFrames.HasAnimation("attack"))
			_sprite.Play("attack");
	}

	private void OnAttackFinished()
	{
		_isAttacking = false;
		CurrentAttack = null;
		_attackHitBox?.SetActive(false);
	}

	public DamageResult TakeDamage(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
	{
		if (_health.IsInvincible || _health.IsDead)
			return DamageResult.Blocked;

		DamageResult result = DamageCalculator.CalculateDamage(
			attackData,
			attackerStats,
			Stats,
			attackerPosition,
			GlobalPosition);

		// Apply damage through HealthComponent (handles invincibility and death)
		_health.ApplyDamage(result.FinalDamage, result.WasCritical);

		// Apply knockback
		if (result.KnockbackApplied != Vector2.Zero)
			Velocity += result.KnockbackApplied;

		if (_health.IsDead)
			result.KilledTarget = true;

		return result;
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
