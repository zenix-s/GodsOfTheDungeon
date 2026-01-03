using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.Systems;

public partial class Player : CharacterBody2D, IGameEntity, IDamageable
{
	private HitBox _attackHitBox;
	private Timer _attackTimer;
	private Timer _invincibilityTimer;
	private AnimatedSprite2D _sprite;

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

	// IDamageable implementation
	public bool IsInvincible { get; private set; }

	public bool IsFacingRight { get; private set; } = true;

	public override void _Ready()
	{
		// Load and clone stats from GameManager
		Stats = GameManager.Instance?.GetPlayerStats()?.Clone() ?? new EntityStats();

		// Setup sprite
		_sprite = GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");

		// Setup invincibility timer
		_invincibilityTimer = new Timer();
		_invincibilityTimer.OneShot = true;
		_invincibilityTimer.Timeout += OnInvincibilityEnded;
		AddChild(_invincibilityTimer);

		_debugLabel = GetNode<Label>("DebugLabel");
		_attackHitBox = GetNodeOrNull<HitBox>("AttackHitBox");

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
		_debugLabel.Text = $"HP: {Stats.CurrentHP}/{Stats.MaxHP}\nVel: {Velocity}";
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
		_attackHitBox?.SetActive(false);
	}

	public DamageResult TakeDamage(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
	{
		if (IsInvincible)
			return DamageResult.Blocked;

		DamageResult result = DamageCalculator.CalculateDamage(
			attackData,
			attackerStats,
			Stats,
			attackerPosition,
			GlobalPosition);

		Stats.CurrentHP -= result.FinalDamage;

		// Apply knockback
		if (result.KnockbackApplied != Vector2.Zero)
			Velocity += result.KnockbackApplied;

		// Start invincibility frames
		StartInvincibility();
		PlayHitEffect();

		if (Stats.IsDead)
		{
			result.KilledTarget = true;
			Die();
		}

		return result;
	}

	private void StartInvincibility()
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

	private void StartInvincibilityVisual()
	{
		if (_sprite != null)
		{
			Tween tween = CreateTween();
			tween.SetLoops((int)(Stats.InvincibilityDuration / 0.1f));
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

	private void Die()
	{
		CanMove = false;
		GameManager.Instance?.OnPlayerDied();
	}

	private void _OnCollectionAreaEntered(Node2D body)
	{
		if (body is ICollectible collectible)
			collectible.Collect(this);
	}
}
