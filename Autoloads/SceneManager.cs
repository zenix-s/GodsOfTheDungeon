using System.Collections.Generic;
using Godot;
using GodsOfTheDungeon.Core.Enums;

public partial class SceneManager : Node
{
    [Signal]
    public delegate void LevelChangeRequestedEventHandler(GameScene targetLevel);

    [Signal]
    public delegate void MenuChangeRequestedEventHandler(MenuScene targetMenu);

    public readonly Dictionary<GameScene, string> LevelPaths = new()
    {
        { GameScene.Playground, "res://Scenes/Levels/Dungeon/Floor1/Floor1.tscn" }
    };

    public readonly Dictionary<MenuScene, string> MenuPaths = new()
    {
        { MenuScene.StartMenu, "res://Scenes/Menus/StartMenu/StartMenu.tscn" }
    };

    public static SceneManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
    }

    public void RequestLevel(GameScene level)
    {
        EmitSignal(SignalName.LevelChangeRequested, Variant.From(level));
    }

    public void RequestMenu(MenuScene menu)
    {
        EmitSignal(SignalName.MenuChangeRequested, Variant.From(menu));
    }
}
