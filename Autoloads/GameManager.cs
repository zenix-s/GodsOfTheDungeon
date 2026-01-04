using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Autoloads;

public class PlayerData
{
    public int Coins { get; set; }
    public EntityStats Stats { get; set; }

    // Health stored separately for persistence (managed by HealthComponent at runtime)
    public int MaxHP { get; set; } = 100;
    public int CurrentHP { get; set; } = 100;
    public float InvincibilityDuration { get; set; } = 0.5f;

    public static PlayerData GetPlayerData()
    {
        return new PlayerData
        {
            Coins = 0,
            MaxHP = 100,
            CurrentHP = 100,
            InvincibilityDuration = 0.5f,
            Stats = new EntityStats
            {
                Attack = 10,
                Defense = 5,
                CriticalChance = 0.1f,
                CriticalMultiplier = 1.5f
            }
        };
    }
}

public partial class GameManager : Node
{
    [Signal]
    public delegate void CoinsChangedEventHandler(int total);

    [Signal]
    public delegate void PlayerDiedEventHandler();

    private PlayerData _playerData;

    public static GameManager Instance { get; private set; }

    public override void _Ready()
    {
        Instance = this;
        _playerData = PlayerData.GetPlayerData();
    }

    public EntityStats GetPlayerStats()
    {
        return _playerData.Stats;
    }

    public PlayerData GetPlayerData()
    {
        return _playerData;
    }

    public void AddCoins(int amount)
    {
        _playerData.Coins += amount;
        EmitSignal(SignalName.CoinsChanged, _playerData.Coins);
    }

    public void OnPlayerDied()
    {
        EmitSignal(SignalName.PlayerDied);
    }
}
