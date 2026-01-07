using Godot;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Handles all movement physics for an entity.
///     Stateless - provides utilities without knowing about game states.
/// </summary>
public partial class MovementComponent : Node
{
    private float _gravity;

    private Vector2 _pendingKnockback;

    // Movement properties (matching current Player.cs values)
    [Export] public float Speed { get; set; } = 300f;
    [Export] public float Acceleration { get; set; } = 1500f;
    [Export] public float Friction { get; set; } = 1200f;
    [Export] public float JumpVelocity { get; set; } = -400f;
    [Export] public float JumpCutMultiplier { get; set; } = 0.5f;
    [Export] public float FallGravityMultiplier { get; set; } = 2.5f;

    // Runtime state
    public Vector2 Velocity { get; set; }
    public float InputDirection { get; private set; }
    public bool IsOnFloor { get; private set; }
    public bool FacingRight { get; private set; } = true;

    public override void _Ready()
    {
        _gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
    }

    /// <summary>
    ///     Read input and update InputDirection and FacingRight.
    ///     Call this each frame before movement calculations.
    /// </summary>
    public void UpdateInput()
    {
        InputDirection = Input.GetAxis("move_left", "move_right");
        if (InputDirection != 0) FacingRight = InputDirection > 0;
    }

    /// <summary>
    ///     Sync velocity and floor state from the CharacterBody2D.
    ///     Call at the start of physics update.
    /// </summary>
    public void UpdateFromOwner(CharacterBody2D owner)
    {
        Velocity = owner.Velocity;
        IsOnFloor = owner.IsOnFloor();

        // Apply any pending knockback after syncing
        if (_pendingKnockback != Vector2.Zero)
        {
            GD.Print(_pendingKnockback);
            Velocity += _pendingKnockback;
            _pendingKnockback = Vector2.Zero;
        }
    }

    /// <summary>
    ///     Apply velocity to owner and call MoveAndSlide.
    ///     Call at the end of physics update.
    /// </summary>
    public void ApplyToOwner(CharacterBody2D owner)
    {
        owner.Velocity = Velocity;
        owner.MoveAndSlide();
        IsOnFloor = owner.IsOnFloor();
    }

    /// <summary>
    ///     Apply gravity based on fall state.
    /// </summary>
    public void ApplyGravity(float delta)
    {
        if (!IsOnFloor)
        {
            float multiplier = Velocity.Y > 0 ? FallGravityMultiplier : 1f;
            Velocity = new Vector2(Velocity.X, Velocity.Y + _gravity * multiplier * delta);
        }
    }

    /// <summary>
    ///     Apply horizontal movement based on InputDirection.
    /// </summary>
    public void ApplyHorizontalMovement(float delta)
    {
        if (InputDirection != 0)
        {
            float targetVelocity = InputDirection * Speed;
            Velocity = new Vector2(
                Mathf.MoveToward(Velocity.X, targetVelocity, Acceleration * delta),
                Velocity.Y
            );
        }
    }

    /// <summary>
    ///     Apply friction when no input and on floor.
    /// </summary>
    public void ApplyFriction(float delta)
    {
        if (InputDirection == 0 && IsOnFloor)
            Velocity = new Vector2(
                Mathf.MoveToward(Velocity.X, 0, Friction * delta),
                Velocity.Y
            );
    }

    /// <summary>
    ///     Apply air friction when no input (less friction than ground).
    /// </summary>
    public void ApplyAirFriction(float delta)
    {
        if (InputDirection == 0)
            Velocity = new Vector2(
                Mathf.MoveToward(Velocity.X, 0, Friction * 0.1f * delta),
                Velocity.Y
            );
    }

    /// <summary>
    ///     Execute a jump.
    /// </summary>
    public void Jump()
    {
        Velocity = new Vector2(Velocity.X, JumpVelocity);
    }

    /// <summary>
    ///     Cut jump velocity (for variable jump height).
    /// </summary>
    public void CutJump()
    {
        if (Velocity.Y < 0) Velocity = new Vector2(Velocity.X, Velocity.Y * JumpCutMultiplier);
    }

    /// <summary>
    ///     Apply knockback force. Stored as pending and applied on next UpdateFromOwner.
    /// </summary>
    public void ApplyKnockback(Vector2 knockback)
    {
        _pendingKnockback += knockback;
    }

    /// <summary>
    ///     Set horizontal velocity directly (for AI movement).
    /// </summary>
    public void SetHorizontalVelocity(float velocity)
    {
        Velocity = new Vector2(velocity, Velocity.Y);
    }

    /// <summary>
    ///     Stop horizontal movement.
    /// </summary>
    public void StopHorizontalMovement()
    {
        Velocity = new Vector2(0, Velocity.Y);
    }

    /// <summary>
    ///     Move toward a target position (for AI).
    ///     Returns the direction (-1, 0, or 1).
    /// </summary>
    public float MoveToward(Vector2 targetPosition, Vector2 currentPosition, float speed)
    {
        float direction = Mathf.Sign(targetPosition.X - currentPosition.X);
        Velocity = new Vector2(direction * speed, Velocity.Y);
        if (direction != 0) FacingRight = direction > 0;
        return direction;
    }
}
