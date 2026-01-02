using Godot;

public partial class Floor1 : Node2D
{
	private RigidBody2D _coinScene;
	private Marker2D _moneyFountain;
	private RandomNumberGenerator _rng = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PackedScene coinSceneResource = GD.Load<PackedScene>("res://Scenes/Prefabs/Interactables/Coin/Coin.tscn");
		_coinScene = coinSceneResource.Instantiate<RigidBody2D>();

		_moneyFountain = GetNode<Marker2D>("MoneyFountain");
		for (int i = 0; i < 10; i++)
		{
			RigidBody2D coinInstance = (RigidBody2D)_coinScene.Duplicate();
			coinInstance.Position =
				_moneyFountain.Position + new Vector2(_rng.RandiRange(-10, 10), _rng.RandiRange(-10, 10));
			AddChild(coinInstance);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
