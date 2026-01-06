using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Player.States;

public partial class PlayerRunState : State
{
    private MovementComponent _movement;
    private AnimationComponent _animation;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        var player = owner as global::Player;
        _movement = player.AliveComponents.Movement;
        _animation = player.AliveComponents.Animation;
    }

    public override void Enter()
    {
        _animation.Play("run");
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

        // Check transitions
        if (!_movement.IsOnFloor)
        {
            TransitionTo("Fall");
            return;
        }

        if (_movement.InputDirection == 0)
        {
            TransitionTo("Idle");
        }
    }

    public override void HandleInput(InputEvent @event)
    {
        if (@event.IsActionPressed("ui_accept") && _movement.IsOnFloor)
        {
            TransitionTo("Jump");
        }
        else if (@event.IsActionPressed("attack_1"))
        {
            TransitionTo("Attack");
        }
    }
}
