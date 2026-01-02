using Godot;
using GodsOfTheDungeon.Core.Interfaces;

public partial class Player : CharacterBody2D
{
	[Export] public float Acceleration = 1500.0f;

	[Export] public bool CanMove;

	// TEST
	public Label debugLabel;

	// Gravity
	[Export] public float FallGravityMultiplier = 2.5f;
	[Export] public float Friction = 1200.0f;
	[Export] public float JumpCutMultiplier = 0.5f;

	// Jumping
	[Export] public float JumpVelocity = -400.0f;


	// Movement
	[Export] public float Speed = 300.0f;

	public override void _Ready()
	{
		debugLabel = GetNode<Label>("DebugLabel");
	}

	public override void _Input(InputEvent @event)
	{
		if (!CanMove) return;

		// Jump on press (grounded only)
		if (@event.IsActionPressed("ui_accept") && IsOnFloor())
		{
			Vector2 velocity = Velocity;
			velocity.Y = JumpVelocity;
			Velocity = velocity;
		}

		// Jump cut on release (while ascending)
		if (@event.IsActionReleased("ui_accept") && Velocity.Y < 0)
		{
			Vector2 velocity = Velocity;
			velocity.Y *= JumpCutMultiplier;
			Velocity = velocity;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!CanMove) return;

		Vector2 velocity = Velocity;
		float deltaF = (float)delta;

		// Apply gravity with fall multiplier when descending
		if (!IsOnFloor())
		{
			float gravityMultiplier = velocity.Y > 0 ? FallGravityMultiplier : 1.0f;
			velocity += GetGravity() * deltaF * gravityMultiplier;
		}

		// Handle horizontal movement
		float direction = GetInputDirection();
		if (direction != 0)
			// Accelerate toward target speed
			velocity.X = Mathf.MoveToward(velocity.X, direction * Speed, Acceleration * deltaF);
		else
			// Apply friction when no input
			velocity.X = Mathf.MoveToward(velocity.X, 0, Friction * deltaF);

		Velocity = velocity;
		debugLabel.Text = $"Vel: {Velocity}";
		MoveAndSlide();
	}

	private float GetInputDirection()
	{
		float direction = 0;
		if (Input.IsActionPressed("ui_left") || Input.IsActionPressed("Left"))
			direction -= 1;
		if (Input.IsActionPressed("ui_right") || Input.IsActionPressed("Right"))
			direction += 1;
		return direction;
	}

	private void _OnCollectionAreaEntered(Node2D body)
	{
		if (body is ICollectible collectible)
		{
			collectible.Collect(this);
		}
	}
}
