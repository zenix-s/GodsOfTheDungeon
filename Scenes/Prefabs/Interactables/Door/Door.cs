using Godot;
using GodsOfTheDungeon.Core.Enums;
using GodsOfTheDungeon.Core.Interfaces;

public partial class Door : StaticBody2D, IInteractable
{
	[Export] public GameScene GameScene;

	public void OnInteract()
	{
		SceneManager.Instance.RequestLevel(GameScene);
	}

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
