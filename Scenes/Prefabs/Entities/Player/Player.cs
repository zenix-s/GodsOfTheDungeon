using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.StateMachine;
using GodsOfTheDungeon.Core.Systems;

public partial class Player : CharacterBody2D, IGameEntity
{
    // System references
    public ComponentManager Components { get; private set; }
    public StateMachine StateMachine { get; private set; }

    // Direct component references (for external access and states)
    public HealthComponent HealthComponent { get; private set; }
    public HurtBoxComponent HurtBoxComponent { get; private set; }
    public AttackHitBoxComponent SlashHitBox { get; private set; }

    private Label _debugLabel;

    [Export] public bool CanMove { get; set; } = true;

    // IGameEntity implementation
    [Export] public EntityStats Stats { get; set; }

    public bool IsFacingRight => Components?.Movement?.FacingRight ?? true;

    public override void _Ready()
    {
        // Load and clone stats from GameManager
        Stats = GameManager.Instance?.GetPlayerStats()?.Clone() ?? new EntityStats();

        // Get systems
        Components = GetNode<ComponentManager>("ComponentManager");
        StateMachine = GetNode<GodsOfTheDungeon.Core.StateMachine.StateMachine>("StateMachine");

        // Get existing components
        HealthComponent = GetNode<HealthComponent>("HealthComponent");
        HurtBoxComponent = GetNode<HurtBoxComponent>("HurtBox");
        SlashHitBox = GetNode<AttackHitBoxComponent>("SlashHitBox");

        // Register external components with manager
        Components.RegisterExternalComponents(HealthComponent, HurtBoxComponent, SlashHitBox);

        // Configure attack hitbox
        SlashHitBox.SetOwnerStats(Stats);
        SlashHitBox.SetActive(false);

        // Setup animation component reference to sprite
        var animationComponent = Components.Animation;
        if (animationComponent != null && animationComponent.Sprite == null)
        {
            animationComponent.Sprite = GetNode<AnimatedSprite2D>("AnimatedSprite2D");
        }

        // Initialize health from GameManager
        SetupHealthComponent();

        // Connect signals
        HurtBoxComponent.HitReceived += OnHitReceived;
        HealthComponent.DamageTaken += OnDamageTaken;
        HealthComponent.Died += OnDied;
        HealthComponent.InvincibilityStarted += OnInvincibilityStarted;
        HealthComponent.InvincibilityEnded += OnInvincibilityEnded;

        _debugLabel = GetNodeOrNull<Label>("DebugLabel");

        // Setup collection area
        var collectionArea = GetNodeOrNull<Area2D>("CollectionArea");
        if (collectionArea != null)
        {
            collectionArea.BodyEntered += _OnCollectionAreaEntered;
        }

        // Initialize state machine LAST (after all components are ready)
        StateMachine.Initialize(this);
    }

    private void SetupHealthComponent()
    {
        PlayerData playerData = GameManager.Instance?.GetPlayerData();
        if (playerData != null)
        {
            HealthComponent.Initialize(
                playerData.MaxHP,
                playerData.CurrentHP,
                playerData.InvincibilityDuration);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        // Debug label update
        if (_debugLabel != null && Components?.Movement != null)
        {
            _debugLabel.Text = $"HP: {HealthComponent.CurrentHP}/{HealthComponent.MaxHP}\n" +
                               $"Vel: {Velocity}\n" +
                               $"State: {StateMachine.CurrentState?.Name}";
        }

        MoveAndSlide();
    }

    private void OnHitReceived(AttackData attackData, EntityStats attackerStats, Vector2 attackerPosition)
    {
        if (HealthComponent.IsInvincible || HealthComponent.IsDead) return;

        DamageResult result = DamageCalculator.CalculateDamage(
            attackData,
            attackerStats,
            Stats,
            attackerPosition,
            GlobalPosition);

        HealthComponent.ApplyDamage(result.FinalDamage, result.WasCritical);
        Components.Movement.ApplyKnockback(result.KnockbackApplied);
    }

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

    public override void _ExitTree()
    {
        if (HealthComponent != null)
        {
            HealthComponent.DamageTaken -= OnDamageTaken;
            HealthComponent.Died -= OnDied;
            HealthComponent.InvincibilityStarted -= OnInvincibilityStarted;
            HealthComponent.InvincibilityEnded -= OnInvincibilityEnded;
        }

        if (HurtBoxComponent != null)
        {
            HurtBoxComponent.HitReceived -= OnHitReceived;
        }

        var collectionArea = GetNodeOrNull<Area2D>("CollectionArea");
        if (collectionArea != null)
        {
            collectionArea.BodyEntered -= _OnCollectionAreaEntered;
        }
    }

    private void StartInvincibilityVisual()
    {
        var sprite = Components?.Animation?.Sprite;
        if (sprite != null)
        {
            Tween tween = CreateTween();
            tween.SetLoops((int)(HealthComponent.InvincibilityDuration / 0.1f));
            tween.TweenProperty(sprite, "modulate:a", 0.5f, 0.05f);
            tween.TweenProperty(sprite, "modulate:a", 1.0f, 0.05f);
        }
    }

    private void StopInvincibilityVisual()
    {
        Components?.Animation?.SetModulate(Colors.White);
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

    private void _OnCollectionAreaEntered(Node2D body)
    {
        if (body is ICollectible collectible)
        {
            collectible.Collect(this);
        }
    }
}
