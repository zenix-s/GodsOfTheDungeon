using Godot;
using GodsOfTheDungeon.Core.Enums;

public partial class Main : Node2D
{
	private Camera2D _camera;
	private Node _entitiesContainer;
	private bool _onMenu;
	private CharacterBody2D _player; // Or CharacterBody3D
	private Node _worldContainer;

	public override void _Ready()
	{
		_worldContainer = GetNode("WorldContainer");
		_player = GetNode<CharacterBody2D>("Entities/Player");
		_camera = GetNode<Camera2D>("Camera");
		_entitiesContainer = GetNode<Node2D>("Entities");

		// Connect to the Autoload Signal
		SceneManager.Instance.LevelChangeRequested += OnLevelChangeRequested;

		// Load initial level
		// OnLevelChangeRequested(GameScene.StartMenu);

		SceneManager.Instance.MenuChangeRequested += OnMenuChangeRequested;
		OnMenuChangeRequested(MenuScene.StartMenu);
		_onMenu = true;
	}

	private void OnLevelChangeRequested(GameScene level)
	{
		// 1. Clean up old level
		foreach (Node child in _worldContainer.GetChildren()) child.QueueFree();

		// 2. Instantiate new level
		string path = SceneManager.Instance.LevelPaths[level];
		PackedScene levelScene = GD.Load<PackedScene>(path);
		Node levelInstance = levelScene.Instantiate();

		// 3. Add to scene
		_worldContainer.AddChild(levelInstance);

		// 4. Move player to spawn point
		// Note: Ensure your Level scenes have a Marker2D named "SpawnPoint"
		Marker2D spawnPoint = levelInstance.GetNode<Marker2D>("SpawnPoint");
		_player.GlobalPosition = spawnPoint.GlobalPosition;
		_player.Set("CanMove", true);

		// 5. Show entities when in level
		_entitiesContainer.Set("visible", true);

		_onMenu = false;
	}

	private void OnMenuChangeRequested(MenuScene menu)
	{
		// 1. Clean up old menu
		foreach (Node child in _worldContainer.GetChildren()) child.QueueFree();

		// 2. Instantiate new menu
		string path = SceneManager.Instance.MenuPaths[menu];
		PackedScene menuScene = GD.Load<PackedScene>(path);
		Node menuInstance = menuScene.Instantiate();

		// 3. Add to scene
		_worldContainer.AddChild(menuInstance);

		// 4. Hide entities when in menu
		_entitiesContainer.Set("visible", false);

		// 5. Set player position on center to keep camera on menu
		Marker2D spawnPoint = menuInstance.GetNode<Marker2D>("SpawnPoint");
		_player.GlobalPosition = spawnPoint.GlobalPosition;
		_player.Set("CanMove", false);

		_camera.GlobalPosition = spawnPoint.GlobalPosition;

		_onMenu = true;
	}

	public override void _Process(double delta)
	{
		if (!_onMenu) CameraFollowPlayer();
	}


	private void CameraFollowPlayer()
	{
		_camera.GlobalPosition = _player.GlobalPosition;
	}
}
