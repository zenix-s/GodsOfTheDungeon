using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Enemies.Slime.States;

public partial class SlimeIdleState : State
{
    private MovementComponent _movement;
    private AnimationComponent _animation;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        var slime = owner as global::Slime;
        _movement = slime.AliveComponents.Movement;
        _animation = slime.AliveComponents.Animation;
    }

    public override void Enter()
    {
        _animation.Play("idle");
        _movement.StopHorizontalMovement();
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        _movement.UpdateFromOwner(Owner);
        _movement.ApplyGravity(deltaF);
        _movement.ApplyToOwner(Owner);
    }
}
