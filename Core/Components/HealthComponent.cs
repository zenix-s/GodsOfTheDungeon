using System;
using Godot;

namespace GodsOfTheDungeon.Core.Components;

public partial class HealthComponent : Node
{
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
	///     Apply damage to this health component.
	///     Returns true if damage was applied, false if blocked (invincible or dead).
	/// </summary>
	public bool ApplyDamage(int damage, bool wasCritical = false)
	{
		if (IsDead || IsInvincible || damage <= 0)
			return false;

		CurrentHP = Mathf.Max(0, CurrentHP - damage);

		DamageTaken?.Invoke(damage, wasCritical);
		HealthChanged?.Invoke(CurrentHP, MaxHP);

		if (InvincibilityDuration > 0)
			StartInvincibility();

		if (IsDead)
			Died?.Invoke();

		return true;
	}

	/// <summary>
	///     Heal the entity by the specified amount.
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
			Healed?.Invoke(actualHealed);
			HealthChanged?.Invoke(CurrentHP, MaxHP);
		}
	}

	/// <summary>
	///     Set current HP to a specific value.
	/// </summary>
	public void SetHP(int hp)
	{
		CurrentHP = Mathf.Clamp(hp, 0, MaxHP);
		HealthChanged?.Invoke(CurrentHP, MaxHP);

		if (IsDead)
			Died?.Invoke();
	}

	/// <summary>
	///     Reset HP to maximum.
	/// </summary>
	public void ResetToMax()
	{
		CurrentHP = MaxHP;
		HealthChanged?.Invoke(CurrentHP, MaxHP);
	}

	/// <summary>
	///     Manually start invincibility frames.
	/// </summary>
	public void StartInvincibility(float? customDuration = null)
	{
		float duration = customDuration ?? InvincibilityDuration;
		if (duration <= 0)
			return;

		IsInvincible = true;
		_invincibilityTimer.WaitTime = duration;
		_invincibilityTimer.Start();

		InvincibilityStarted?.Invoke();
	}

	/// <summary>
	///     Manually end invincibility frames.
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
		InvincibilityEnded?.Invoke();
	}

	/// <summary>
	///     Initialize health with specific values (useful for save/load).
	/// </summary>
	public void Initialize(int maxHP, int currentHP, float invincibilityDuration = 0f)
	{
		MaxHP = maxHP;
		CurrentHP = currentHP;
		InvincibilityDuration = invincibilityDuration;
		HealthChanged?.Invoke(CurrentHP, MaxHP);
	}

	#region Events

	public event Action<int, int> HealthChanged; // (currentHP, maxHP)
	public event Action<int, bool> DamageTaken; // (damage, wasCritical)
	public event Action<int> Healed; // (amount)
	public event Action Died;
	public event Action InvincibilityStarted;
	public event Action InvincibilityEnded;

	#endregion

	#region Exports

	[Export] public int MaxHP { get; set; } = 100;
	[Export] public float InvincibilityDuration { get; set; }
	[Export] public bool StartAtMaxHP { get; set; } = true;

	#endregion

	#region Properties

	public int CurrentHP { get; private set; }
	public bool IsDead => CurrentHP <= 0;
	public bool IsInvincible { get; private set; }
	public float HealthPercentage => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;

	#endregion
}
