using Godot;

namespace GodsOfTheDungeon.Core.Components;

public partial class HealthComponent : Node
{
	#region Signals
	[Signal]
	public delegate void HealthChangedEventHandler(int currentHP, int maxHP);

	[Signal]
	public delegate void DamageTakenEventHandler(int damage, bool wasCritical);

	[Signal]
	public delegate void HealedEventHandler(int amount);

	[Signal]
	public delegate void DiedEventHandler();

	[Signal]
	public delegate void InvincibilityStartedEventHandler();

	[Signal]
	public delegate void InvincibilityEndedEventHandler();
	#endregion

	#region Exports
	[Export] public int MaxHP { get; set; } = 100;
	[Export] public float InvincibilityDuration { get; set; } = 0f;
	[Export] public bool StartAtMaxHP { get; set; } = true;
	#endregion

	#region Properties
	public int CurrentHP { get; private set; }
	public bool IsDead => CurrentHP <= 0;
	public bool IsInvincible { get; private set; }
	public float HealthPercentage => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;
	#endregion

	private Timer _invincibilityTimer;

	public override void _Ready()
	{
		if (StartAtMaxHP)
			CurrentHP = MaxHP;

		SetupInvincibilityTimer();
	}

	private void SetupInvincibilityTimer()
	{
		_invincibilityTimer = new Timer();
		_invincibilityTimer.OneShot = true;
		_invincibilityTimer.Timeout += OnInvincibilityEnded;
		AddChild(_invincibilityTimer);
	}

	/// <summary>
	/// Apply damage to this health component.
	/// Returns true if damage was applied, false if blocked (invincible or dead).
	/// </summary>
	public bool ApplyDamage(int damage, bool wasCritical = false)
	{
		if (IsDead || IsInvincible || damage <= 0)
			return false;

		CurrentHP = Mathf.Max(0, CurrentHP - damage);

		EmitSignal(SignalName.DamageTaken, damage, wasCritical);
		EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);

		if (InvincibilityDuration > 0)
			StartInvincibility();

		if (IsDead)
			EmitSignal(SignalName.Died);

		return true;
	}

	/// <summary>
	/// Heal the entity by the specified amount.
	/// </summary>
	public void Heal(int amount)
	{
		if (IsDead || amount <= 0)
			return;

		int previousHP = CurrentHP;
		CurrentHP = Mathf.Min(CurrentHP + amount, MaxHP);
		int actualHealed = CurrentHP - previousHP;

		if (actualHealed > 0)
		{
			EmitSignal(SignalName.Healed, actualHealed);
			EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
		}
	}

	/// <summary>
	/// Set current HP to a specific value.
	/// </summary>
	public void SetHP(int hp)
	{
		CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
		EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);

		if (IsDead)
			EmitSignal(SignalName.Died);
	}

	/// <summary>
	/// Reset HP to maximum.
	/// </summary>
	public void ResetToMax()
	{
		CurrentHP = MaxHP;
		EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
	}

	/// <summary>
	/// Manually start invincibility frames.
	/// </summary>
	public void StartInvincibility(float? customDuration = null)
	{
		float duration = customDuration ?? InvincibilityDuration;
		if (duration <= 0)
			return;

		IsInvincible = true;
		_invincibilityTimer.WaitTime = duration;
		_invincibilityTimer.Start();

		EmitSignal(SignalName.InvincibilityStarted);
	}

	/// <summary>
	/// Manually end invincibility frames.
	/// </summary>
	public void EndInvincibility()
	{
		if (!IsInvincible)
			return;

		_invincibilityTimer.Stop();
		OnInvincibilityEnded();
	}

	private void OnInvincibilityEnded()
	{
		IsInvincible = false;
		EmitSignal(SignalName.InvincibilityEnded);
	}

	/// <summary>
	/// Initialize health with specific values (useful for save/load).
	/// </summary>
	public void Initialize(int maxHP, int currentHP, float invincibilityDuration = 0f)
	{
		MaxHP = maxHP;
		CurrentHP = currentHP;
		InvincibilityDuration = invincibilityDuration;
		EmitSignal(SignalName.HealthChanged, CurrentHP, MaxHP);
	}
}
