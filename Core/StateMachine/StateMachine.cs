using System.Collections.Generic;
using Godot;

namespace GodsOfTheDungeon.Core.StateMachine;

/// <summary>
///     Manages state transitions for a CharacterBody2D entity.
///     Add State nodes as children and set InitialState in the editor.
/// </summary>
public partial class StateMachine : Node
{
    private readonly Dictionary<StringName, State> _states = new();
    private bool _isInitialized;
    private CharacterBody2D _owner;
    [Export] public State InitialState { get; set; }

    public State CurrentState { get; private set; }

    /// <summary>
    ///     Call from parent entity's _Ready() to initialize all states.
    /// </summary>
    public void Initialize(CharacterBody2D owner)
    {
        _owner = owner;

        // Collect all child states
        foreach (Node child in GetChildren())
            if (child is State state)
            {
                _states[child.Name] = state;
                state.Initialize(owner, this);
            }

        // Enter initial state
        if (InitialState != null)
        {
            CurrentState = InitialState;
            CurrentState.Enter();
        }
        else if (_states.Count > 0)
        {
            GD.PushWarning($"StateMachine: No InitialState set on {owner.Name}");
        }

        _isInitialized = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!_isInitialized) return;
        CurrentState?.PhysicsUpdate(delta);
    }

    public override void _Input(InputEvent @event)
    {
        if (!_isInitialized) return;
        CurrentState?.HandleInput(@event);
    }

    /// <summary>
    ///     Transition to a new state by name.
    /// </summary>
    public void TransitionTo(StringName stateName)
    {
        if (!_states.TryGetValue(stateName, out State newState))
        {
            GD.PushError($"StateMachine: State '{stateName}' not found");
            return;
        }

        TransitionTo(newState);
    }

    /// <summary>
    ///     Transition to a new state by reference.
    /// </summary>
    public void TransitionTo(State newState)
    {
        if (newState == CurrentState) return;

        CurrentState?.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    /// <summary>
    ///     Get a state by name.
    /// </summary>
    public State GetState(StringName stateName)
    {
        return _states.GetValueOrDefault(stateName);
    }
}
