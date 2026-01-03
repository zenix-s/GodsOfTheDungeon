namespace GodsOfTheDungeon.Core.Interfaces;

/// <summary>
///     Interface for enemies that can detect the player.
///     Enemies should extend GameEntity directly.
/// </summary>
public interface IEnemy
{
    void OnPlayerDetected(Player player);
    void OnPlayerLost();
}
