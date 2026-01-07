using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.Data;
using GodsOfTheDungeon.Core.Interfaces;
using GodsOfTheDungeon.Core.StateMachine;

public partial class Player : CharacterBody2D, IGameEntity
{
    private Label _debugLabel;

    // System references
    public AliveEntityComponentManager AliveComponents { get; private set; }
    public StateMachine StateMachine { get; private set; }

    // Direct component references (for external access and states)
    public HealthComponent HealthComponent { get; private set; }
    public AttackHitBoxComponent SlashHitBox { get; private set; }

    [Export] public bool CanMove { get; set; } = true;

    public bool IsFacingRight => AliveComponents?.Movement?.FacingRight ?? true;

    // IGameEntity implementation
    [Export] public EntityStats Stats { get; set; }

    public override void _Ready()
    {
        // Load and clone stats from GameManager
        Stats = GameManager.Instance?.GetPlayerStats()?.Clone() ?? new EntityStats();

        // Get systems
        AliveComponents = GetNode<AliveEntityComponentManager>("ComponentManager");
        StateMachine = GetNode<StateMachine>("StateMachine");

        // Get direct component references
        HealthComponent = AliveComponents.Health;
        SlashHitBox = GetNode<AttackHitBoxComponent>("SlashHitBox");

        // Configure attack hitbox
        SlashHitBox.SetOwnerStats(Stats);
        SlashHitBox.SetActive(false);

        // Initialize health from GameManager
        SetupHealthComponent();

        // Connect to AliveEntityComponentManager signals
        AliveComponents.DamageTaken += OnDamageTaken;
        AliveComponents.Died += OnDied;
        AliveComponents.InvincibilityStarted += OnInvincibilityStarted;
        AliveComponents.InvincibilityEnded += OnInvincibilityEnded;

        _debugLabel = GetNodeOrNull<Label>("DebugLabel");

        // Setup collection area
        Area2D collectionArea = GetNodeOrNull<Area2D>("CollectionArea");
        if (collectionArea != null) collectionArea.BodyEntered += _OnCollectionAreaEntered;

        // Initialize state machine LAST (after all components are ready)
        StateMachine.Initialize(this);
    }

    private void SetupHealthComponent()
    {
        PlayerData playerData = GameManager.Instance?.GetPlayerData();
        if (playerData != null)
            HealthComponent.Initialize(
                playerData.MaxHP,
                playerData.CurrentHP,
                playerData.InvincibilityDuration);
    }

    public override void _PhysicsProcess(double delta)
    {
        // Debug label update
        if (_debugLabel != null && AliveComponents?.Movement != null)
            _debugLabel.Text = $"HP: {HealthComponent.CurrentHP}/{HealthComponent.MaxHP}\n" +
                               $"Vel: {Velocity}\n" +
                               $"State: {StateMachine.CurrentState?.Name}";

        MoveAndSlide();
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
        if (AliveComponents != null)
        {
            AliveComponents.DamageTaken -= OnDamageTaken;
            AliveComponents.Died -= OnDied;
            AliveComponents.InvincibilityStarted -= OnInvincibilityStarted;
            AliveComponents.InvincibilityEnded -= OnInvincibilityEnded;
        }

        Area2D collectionArea = GetNodeOrNull<Area2D>("CollectionArea");
        if (collectionArea != null) collectionArea.BodyEntered -= _OnCollectionAreaEntered;
    }

    private void StartInvincibilityVisual()
    {
        AnimatedSprite2D sprite = AliveComponents?.Animation?.Sprite;
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
        AliveComponents?.Animation?.SetModulate(Colors.White);
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

    private void _OnCollectionAreaEntered(Node2D body)
    {
        if (body is ICollectible collectible) collectible.Collect(this);
    }
}
