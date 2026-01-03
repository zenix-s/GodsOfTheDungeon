using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Entities;
using GodsOfTheDungeon.Core.Interfaces;

public partial class Player : GameEntity
{
	private HitBox _attackHitBox;
	private Timer _attackTimer;

	// Internal state
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

	// Combat exports - 3 attacks on J, K, L keys
	[Export] public AttackData Attack1Data { get; set; }
	[Export] public AttackData Attack2Data { get; set; }
	[Export] public AttackData Attack3Data { get; set; }
	[Export] public float AttackDuration = 0.3f;

	public bool IsFacingRight { get; private set; } = true;

	public override void _Ready()
	{
		// Load stats from GameManager before calling base._Ready()
		Stats = GameManager.Instance?.GetPlayerStats()?.Clone() ?? new EntityStats();

		base._Ready();

		_debugLabel = GetNode<Label>("DebugLabel");
		_attackHitBox = GetNodeOrNull<HitBox>("AttackHitBox");

		SetupAttackTimer();
		SetupDefaultAttacks();
	}

	private void SetupAttackTimer()
	{
		_attackTimer = new Timer();
		_attackTimer.OneShot = true;
		_attackTimer.Timeout += OnAttackFinished;
		AddChild(_attackTimer);
	}

	private void SetupDefaultAttacks()
	{
		// Default attack data if not assigned in editor
		Attack1Data ??= new AttackData
		{
			AttackName = "Slash",
			BaseDamage = 1,
			KnockbackForce = 200f
		};

		Attack2Data ??= new AttackData
		{
			AttackName = "Thrust",
			BaseDamage = 2,
			KnockbackForce = 100f
		};

		Attack3Data ??= new AttackData
		{
			AttackName = "Heavy Swing",
			BaseDamage = 3,
			KnockbackForce = 350f,
			CanCrit = true
		};
	}

	public override void _Input(InputEvent @event)
	{
		if (!CanMove) return;

		// Jump on press (grounded only)
		if (@event.IsActionPressed("ui_accept") && IsOnFloor())
		{
			Vector2 velocity = Velocity;
			velocity.Y = JumpVelocity;
			Velocity = velocity;
		}

		// Jump cut on release (while ascending)
		if (@event.IsActionReleased("ui_accept") && Velocity.Y < 0)
		{
			Vector2 velocity = Velocity;
			velocity.Y *= JumpCutMultiplier;
			Velocity = velocity;
		}

		// Attack inputs - J, K, L
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

		// Apply gravity with fall multiplier when descending
		if (!IsOnFloor())
		{
			float gravityMultiplier = velocity.Y > 0 ? FallGravityMultiplier : 1.0f;
			velocity += GetGravity() * deltaF * gravityMultiplier;
		}

		// Handle horizontal movement
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

		// Update sprite facing
		Sprite.FlipH = !IsFacingRight;

		// Update attack hitbox position based on facing direction
		if (_attackHitBox != null)
			_attackHitBox.Position = new Vector2(IsFacingRight ? 20 : -20, 0);

		// Animation handling
		if (!_isAttacking)
		{
			if (IsOnFloor())
				Sprite.Play(direction == 0 ? "idle" : "run");
			else
				Sprite.Play("jump");
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

		// Set the attack data and activate hitbox
		_attackHitBox.SetAttack(attackData);
		_attackHitBox.SetActive(true);

		// Start timer to end attack
		_attackTimer.WaitTime = AttackDuration;
		_attackTimer.Start();

		// Play attack animation if exists
		if (Sprite.SpriteFrames.HasAnimation("attack"))
			Sprite.Play("attack");
	}

	private void OnAttackFinished()
	{
		_isAttacking = false;
		_attackHitBox?.SetActive(false);
	}

	public override DamageResult TakeDamage(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
	{
		DamageResult result = base.TakeDamage(attackData, attackerStats, attackerPosition);

		// Notify GameManager specifically for player damage
		GameManager.Instance?.OnPlayerDamaged(result.FinalDamage, Stats.CurrentHP, Stats.MaxHP);

		return result;
	}

	public override void Die()
	{
		base.Die();
		CanMove = false;
		GameManager.Instance?.OnPlayerDied();
	}

	private void _OnCollectionAreaEntered(Node2D body)
	{
		if (body is ICollectible collectible)
			collectible.Collect(this);
	}
}
