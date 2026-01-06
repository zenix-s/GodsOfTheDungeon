using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Enemies.Slime.States;

public partial class SlimeChaseState : State
{
    private MovementComponent _movement;
    private AnimationComponent _animation;
    private global::Slime _slime;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        _slime = owner as global::Slime;
        _movement = _slime.Components.Movement;
        _animation = _slime.Components.Animation;
    }

    public override void Enter()
    {
        _animation.Play("idle"); // Could use a walk/chase animation if available
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        if (_slime.TargetPlayer == null)
        {
            TransitionTo("Idle");
            return;
        }

        _movement.UpdateFromOwner(Owner);

        // Move toward player
        _movement.MoveToward(
            _slime.TargetPlayer.GlobalPosition,
            Owner.GlobalPosition,
            _slime.ChaseSpeed);

        _movement.ApplyGravity(deltaF);
        _movement.ApplyToOwner(Owner);

        // Update sprite facing
        _animation.SetFlipH(!_movement.FacingRight);

        // Check if in attack range
        float distanceToPlayer = Owner.GlobalPosition.DistanceTo(_slime.TargetPlayer.GlobalPosition);
        if (distanceToPlayer <= _slime.AttackRange && _slime.CanAttack)
        {
            TransitionTo("Attack");
        }
    }
}
