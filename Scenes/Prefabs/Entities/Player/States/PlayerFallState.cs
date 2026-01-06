using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Player.States;

public partial class PlayerFallState : State
{
    private MovementComponent _movement;
    private AnimationComponent _animation;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        var player = owner as global::Player;
        _movement = player.Components.Movement;
        _animation = player.Components.Animation;
    }

    public override void Enter()
    {
        _animation.Play("jump"); // Could use separate "fall" animation if available
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        _movement.UpdateFromOwner(Owner);
        _movement.UpdateInput();

        _movement.ApplyGravity(deltaF);
        _movement.ApplyHorizontalMovement(deltaF);

        _movement.ApplyToOwner(Owner);

        _animation.SetFlipH(!_movement.FacingRight);

        // Check for landing
        if (_movement.IsOnFloor)
        {
            if (_movement.InputDirection != 0)
            {
                TransitionTo("Run");
            }
            else
            {
                TransitionTo("Idle");
            }
        }
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("attack_1"))
        {
            TransitionTo("Attack");
        }
    }
}
