using Godot;
using GodsOfTheDungeon.Core.Components;
using GodsOfTheDungeon.Core.StateMachine;

namespace GodsOfTheDungeon.Scenes.Prefabs.Entities.Player.States;

public partial class PlayerAttackState : State
{
    private const string SlashAnimation = "attack_slash";
    private const float AttackDistance = 20f;
    private AnimationComponent _animation;
    private bool _attackFinished;
    private Timer _attackTimer;
    private AnimatedSprite2D _effectSprite;

    private MovementComponent _movement;
    private AttackHitBoxComponent _slashHitBox;

    [Export] public float AttackDuration { get; set; } = 0.25f;

    public override void Initialize(CharacterBody2D owner, StateMachine stateMachine)
    {
        base.Initialize(owner, stateMachine);

        global::Player player = owner as global::Player;
        _movement = player.AliveComponents.Movement;
        _animation = player.AliveComponents.Animation;
        _slashHitBox = player.SlashHitBox;

        // Create timer as child of this state
        _attackTimer = new Timer();
        _attackTimer.OneShot = true;
        _attackTimer.Timeout += OnAttackTimerTimeout;
        AddChild(_attackTimer);
    }

    public override void Enter()
    {
        _attackFinished = false;
        _movement.UpdateFromOwner(Owner);

        // Get attack direction towards mouse
        Vector2 mousePos = Owner.GetGlobalMousePosition();
        Vector2 direction = (mousePos - Owner.GlobalPosition).Normalized();

        // Position and rotate hitbox
        _slashHitBox.Position = direction * AttackDistance;
        _slashHitBox.Rotation = direction.Angle();
        _slashHitBox.SetActive(true);

        // Start attack timer
        _attackTimer.WaitTime = AttackDuration;
        _attackTimer.Start();

        // Play character animation
        if (_animation.HasAnimation(SlashAnimation)) _animation.PlayOnce(SlashAnimation);

        // Show and play hitbox effect sprite
        _effectSprite = _slashHitBox.GetNodeOrNull<AnimatedSprite2D>("AnimatedSprite2D");
        if (_effectSprite != null)
        {
            _effectSprite.Visible = true;
            _effectSprite.Play();
        }
    }

    public override void Exit()
    {
        _slashHitBox.SetActive(false);
        _attackTimer.Stop();

        if (_effectSprite != null)
        {
            _effectSprite.Stop();
            _effectSprite.Visible = false;
            _effectSprite = null;
        }
    }

    public override void PhysicsUpdate(double delta)
    {
        float deltaF = (float)delta;

        _movement.UpdateFromOwner(Owner);
        _movement.UpdateInput();

        // Apply gravity and allow horizontal movement during attack
        _movement.ApplyGravity(deltaF);
        _movement.ApplyHorizontalMovement(deltaF);
        _movement.ApplyFriction(deltaF);

        _movement.ApplyToOwner(Owner);

        // Check for attack completion
        if (_attackFinished)
        {
            if (!_movement.IsOnFloor)
                TransitionTo("Fall");
            else if (_movement.InputDirection != 0)
                TransitionTo("Run");
            else
                TransitionTo("Idle");
        }
    }

    private void OnAttackTimerTimeout()
    {
        _attackFinished = true;
    }
}
