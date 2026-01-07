using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Player.States;

public partial class PlayerJumpState : State
{
    private AnimationComponent _animation;
    private MovementComponent _movement;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        global::Player player = owner as global::Player;
        _movement = player.AliveComponents.Movement;
        _animation = player.AliveComponents.Animation;
    }

    public override void Enter()
    {
        _animation.Play("jump");

        // Get current velocity from owner before jumping
        _movement.UpdateFromOwner(Owner);
        _movement.Jump();
        _movement.ApplyToOwner(Owner);
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

        // Transition to Fall when ascending stops
        if (_movement.Velocity.Y >= 0) TransitionTo("Fall");
    }

    public override void HandleInput(InputEvent @event)
    {
        // Variable jump height - release early for shorter jump
        if (@event.IsActionReleased("ui_accept"))
        {
            _movement.UpdateFromOwner(Owner);
            _movement.CutJump();
            _movement.ApplyToOwner(Owner);
        }
        else if (@event.IsActionPressed("attack_1"))
        {
            TransitionTo("Attack");
        }
    }
}
