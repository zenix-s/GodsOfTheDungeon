using Godot;
using GodsOfTheDungeon.Core.Data;

namespace GodsOfTheDungeon.Autoloads;

public class PlayerData
{
    public int Coins { get; set; }
    public EntityStats Stats { get; set; }

    public static PlayerData GetPlayerData()
    {
        return new PlayerData
        {
            Coins = 0,
            Stats = new EntityStats
            {
                MaxHP = 100,
                CurrentHP = 100,
                Attack = 10,
                Defense = 5,
                CriticalChance = 0.1f,
                CriticalMultiplier = 1.5f,
                InvincibilityDuration = 0.5f
            }
        };
    }
}

public partial class GameManager : Node
{
    // Existing signals
    [Signal]
    public delegate void CoinsChangedEventHandler(int total);

    [Signal]
    public delegate void EnemyKilledEventHandler(Node enemy);

    // Combat signals
    [Signal]
    public delegate void EntityDamagedEventHandler(Node entity, int damage, bool wasCritical);

    [Signal]
    public delegate void EntityDiedEventHandler(Node entity);

    [Signal]
    public delegate void PlayerDamagedEventHandler(int damage, int currentHP, int maxHP);

    [Signal]
    public delegate void PlayerDiedEventHandler();

    // Global data
    private PlayerData _playerData;

    // Singleton instance
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

    public void AddCoins(int amount)
    {
        _playerData.Coins += amount;
        EmitSignal(SignalName.CoinsChanged, _playerData.Coins);
    }

    public void OnEntityDamaged(Node entity, int damage, bool wasCritical)
    {
        EmitSignal(SignalName.EntityDamaged, entity, damage, wasCritical);
    }

    public void OnEntityDied(Node entity)
    {
        EmitSignal(SignalName.EntityDied, entity);
    }

    public void OnPlayerDamaged(int damage, int currentHP, int maxHP)
    {
        EmitSignal(SignalName.PlayerDamaged, damage, currentHP, maxHP);
    }

    public void OnPlayerDied()
    {
        EmitSignal(SignalName.PlayerDied);
    }

    public void OnEnemyKilled(Node enemy)
    {
        EmitSignal(SignalName.EnemyKilled, enemy);
    }
}
