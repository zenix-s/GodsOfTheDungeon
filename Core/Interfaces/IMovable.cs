using Godot;

namespace GodsOfTheDungeon.Core.Interfaces;

/// <summary>
///     Interface for entities with movement capabilities.
/// </summary>
public interface IMovable
{
    float Speed { get; }
    Vector2 Velocity { get; set; }
    bool CanMove { get; set; }
    void Move(Vector2 direction, float delta);
}
