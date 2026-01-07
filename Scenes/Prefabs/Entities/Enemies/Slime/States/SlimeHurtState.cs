using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Enemies.Slime.States;

public partial class SlimeHurtState : State
{
    private AnimationComponent _animation;

    private MovementComponent _movement;
    private float _stunTimer;
    [Export] public float StunDuration { get; set; } = 0.3f;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        global::Slime slime = owner as global::Slime;
        _movement = slime.AliveComponents.Movement;
        _animation = slime.AliveComponents.Animation;
    }

    public override void Enter()
    {
        _stunTimer = StunDuration;
        _animation.Play("idle"); // Use hurt animation if available
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        _movement.UpdateFromOwner(Owner);
        _movement.ApplyGravity(deltaF);
        _movement.ApplyFriction(deltaF);
        _movement.ApplyToOwner(Owner);

        _stunTimer -= deltaF;
        if (_stunTimer <= 0)
        {
            global::Slime slime = Owner as global::Slime;
            if (slime.IsPlayerInRange)
                TransitionTo("Chase");
            else
                TransitionTo("Idle");
        }
    }
}
