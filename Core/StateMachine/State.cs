using Godot;

namespace GodsOfTheDungeon.Core.StateMachine;

/// <summary>
/// Abstract base class for all entity states.
/// Extend this class to create specific states for Player, enemies, etc.
/// </summary>
public abstract partial class State : Node
{
    /// <summary>
    /// The CharacterBody2D that owns this state machine.
    /// </summary>
    protected new CharacterBody2D Owner { get; private set; }

    /// <summary>
    /// Reference to the parent StateMachine for transitions.
    /// </summary>
    protected StateMachine StateMachine { get; private set; }

    /// <summary>
    /// Called once when the StateMachine initializes all states.
    /// Override to cache component references.
    /// </summary>
    public virtual void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        Owner = owner;
        StateMachine = stateMachine;
    }

    /// <summary>
    /// Called when entering this state.
    /// </summary>
    public virtual void Enter() { }

    /// <summary>
    /// Called when exiting this state.
    /// </summary>
    public virtual void Exit() { }

    /// <summary>
    /// Called every physics frame while this state is active.
    /// </summary>
    public virtual void PhysicsUpdate(double delta) { }

    /// <summary>
    /// Called for input events while this state is active.
    /// </summary>
    public virtual void HandleInput(InputEvent @event) { }

    /// <summary>
    /// Helper method to request a state transition by name.
    /// </summary>
    protected void TransitionTo(StringName stateName)
    {
        StateMachine.TransitionTo(stateName);
    }
}
