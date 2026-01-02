using Godot;

namespace GodsOfTheDungeon.Autoloads;

public class PlayerData
{
    public int Coins { get; set; } = 0;

    public static PlayerData GetPlayerData()
    {
        // TODO: Cargar datos del jugador desde persistencia
        return new PlayerData();
    }
}

public partial class GameManager : Node
{
    // Singleton instance
    public static GameManager Instance { get; private set; }

    // Signals
    [Signal] public delegate void CoinsChangedEventHandler(int total);

    // Global entities
    private PlayerData _playerData;

    public override void _Ready()
    {
        Instance = this;

        _playerData = PlayerData.GetPlayerData();
    }

    public void AddCoins(int amount)
    {
        _playerData.Coins += amount;
        EmitSignal(SignalName.CoinsChanged, _playerData.Coins);
    }
}
