using Godot;
using GodsOfTheDungeon.Core.Enums;

public partial class StartButton : Button
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _Pressed()
    {
        SceneManager.Instance.RequestLevel(GameScene.Playground);
    }
}
