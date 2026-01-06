using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Enemies.Slime.States;

public partial class SlimeAttackState : State
{
    private const float AttackDuration = 0.2f;

    private MovementComponent _movement;
    private AnimationComponent _animation;
    private global::Slime _slime;
    private Timer _attackTimer;
    private bool _attackFinished;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        _slime = owner as global::Slime;
        _movement = _slime.Components.Movement;
        _animation = _slime.Components.Animation;

        // Create timer as child of this state
        _attackTimer = new Timer();
        _attackTimer.OneShot = true;
        _attackTimer.Timeout += OnAttackTimerTimeout;
        AddChild(_attackTimer);
    }

    public override void Enter()
    {
        _attackFinished = false;

        // Activate hitbox
        _slime.AttackHitBox?.SetActive(true);

        // Start attack duration timer
        _attackTimer.WaitTime = AttackDuration;
        _attackTimer.Start();

        // Mark attack as used (cooldown handled by Slime)
        _slime.StartAttackCooldown();
    }

    public override void Exit()
    {
        _slime.AttackHitBox?.SetActive(false);
        _attackTimer.Stop();
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        _movement.UpdateFromOwner(Owner);
        _movement.ApplyGravity(deltaF);
        _movement.StopHorizontalMovement();
        _movement.ApplyToOwner(Owner);

        if (_attackFinished)
        {
            // Return to chase or idle based on player presence
            if (_slime.IsPlayerInRange && _slime.TargetPlayer != null)
            {
                TransitionTo("Chase");
            }
            else
            {
                TransitionTo("Idle");
            }
        }
    }

    private void OnAttackTimerTimeout()
    {
        _attackFinished = true;
    }
}
