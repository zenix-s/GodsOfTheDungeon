using Godot;
using GodsOfTheDungeon.Autoloads;
using GodsOfTheDungeon.Core.Interfaces;

public partial class Coin : RigidBody2D, ICollectible
{
	[Export] public int Value = 1;

	public void Collect(Player player)
	{
		GameManager.Instance.AddCoins(Value);
		QueueFree();
	}
}
