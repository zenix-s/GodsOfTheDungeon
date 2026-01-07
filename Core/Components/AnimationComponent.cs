using Godot;

namespace GodsOfTheDungeon.Core.Components;

/// <summary>
///     Wrapper for AnimatedSprite2D with convenience methods.
/// </summary>
public partial class AnimationComponent : Node
{
    [Signal]
    public delegate void AnimationFinishedEventHandler(StringName animName);

    [Export] public AnimatedSprite2D Sprite { get; set; }

    public StringName CurrentAnimation { get; private set; }
    public bool IsPlaying => Sprite?.IsPlaying() ?? false;

    public override void _Ready()
    {
        ValidateRequirements();
        Sprite.AnimationFinished += OnAnimationFinished;
    }

    /// <summary>
    ///     Validates that all required exported fields are properly assigned.
    ///     Throws an exception if validation fails.
    /// </summary>
    public void ValidateRequirements()
    {
        if (Sprite == null)
            throw new System.InvalidOperationException(
                $"AnimationComponent ({GetPath()}): Sprite is required but not assigned. " +
                "Assign an AnimatedSprite2D in the inspector.");
    }

    /// <summary>
    ///     Play an animation (only if not already playing this animation).
    /// </summary>
    public void Play(StringName animationName)
    {
        if (Sprite == null) return;

        if (CurrentAnimation != animationName)
        {
            CurrentAnimation = animationName;
            Sprite.Play(animationName);
        }
    }

    /// <summary>
    ///     Play an animation once (always restarts, even if same animation).
    ///     AnimationFinished signal fires when done.
    /// </summary>
    public void PlayOnce(StringName animationName)
    {
        if (Sprite == null) return;

        CurrentAnimation = animationName;
        Sprite.Stop();
        Sprite.Play(animationName);
    }

    /// <summary>
    ///     Set horizontal flip (for facing direction).
    /// </summary>
    public void SetFlipH(bool flip)
    {
        if (Sprite != null) Sprite.FlipH = flip;
    }

    /// <summary>
    ///     Check if a specific animation is currently playing.
    /// </summary>
    public bool IsAnimationPlaying(StringName animationName)
    {
        return CurrentAnimation == animationName && Sprite?.IsPlaying() == true;
    }

    /// <summary>
    ///     Check if the sprite has a given animation.
    /// </summary>
    public bool HasAnimation(StringName animationName)
    {
        return Sprite?.SpriteFrames?.HasAnimation(animationName) ?? false;
    }

    /// <summary>
    ///     Stop the current animation.
    /// </summary>
    public void Stop()
    {
        Sprite?.Stop();
    }

    /// <summary>
    ///     Set the modulate color (for effects like hit flash).
    /// </summary>
    public void SetModulate(Color color)
    {
        if (Sprite != null) Sprite.Modulate = color;
    }

    private void OnAnimationFinished()
    {
        EmitSignal(SignalName.AnimationFinished, CurrentAnimation);
    }

    public override void _ExitTree()
    {
        if (Sprite != null) Sprite.AnimationFinished -= OnAnimationFinished;
    }
}
