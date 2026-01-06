using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Player.States;

public partial class PlayerIdleState : State
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
        _animation.Play("idle");
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        _movement.UpdateFromOwner(Owner);
        _movement.UpdateInput();

        _movement.ApplyGravity(deltaF);
        _movement.ApplyFriction(deltaF);

        _movement.ApplyToOwner(Owner);

        // Update sprite facing
        _animation.SetFlipH(!_movement.FacingRight);

        // Check transitions
        if (!_movement.IsOnFloor)
        {
            TransitionTo("Fall");
            return;
        }

        if (_movement.InputDirection != 0)
        {
            GD.Print("move");
            TransitionTo("Run");
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
