using Godot;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
/// Central access point for all entity components.
/// Manages both child components and external (sibling) components.
/// </summary>
public partial class ComponentManager : Node
{
    // Child components (found in _Ready)
    public MovementComponent Movement { get; private set; }
    public AnimationComponent Animation { get; private set; }

    // External components (registered by parent entity)
    public HealthComponent Health { get; private set; }
    public HurtBoxComponent HurtBox { get; private set; }
    public AttackHitBoxComponent AttackHitBox { get; private set; }

    public override void _Ready()
    {
        // Find child components
        Movement = GetNodeOrNull<MovementComponent>("MovementComponent");
        Animation = GetNodeOrNull<AnimationComponent>("AnimationComponent");

        if (Movement == null)
            GD.PushWarning("ComponentManager: MovementComponent not found");
        if (Animation == null)
            GD.PushWarning("ComponentManager: AnimationComponent not found");
    }

    /// <summary>
    /// Register external components that are siblings in the scene tree.
    /// Called by the parent entity in _Ready().
    /// </summary>
    public void RegisterExternalComponents(
        HealthComponent health,
        HurtBoxComponent hurtBox,
        AttackHitBoxComponent attackHitBox)
    {
        Health = health;
        HurtBox = hurtBox;
        AttackHitBox = attackHitBox;
    }

    /// <summary>
    /// Generic getter for component access by type.
    /// </summary>
    public T Get<T>() where T : Node
    {
        return typeof(T).Name switch
        {
            nameof(MovementComponent) => Movement as T,
            nameof(AnimationComponent) => Animation as T,
            nameof(HealthComponent) => Health as T,
            nameof(HurtBoxComponent) => HurtBox as T,
            nameof(AttackHitBoxComponent) => AttackHitBox as T,
            _ => null
        };
    }
}
