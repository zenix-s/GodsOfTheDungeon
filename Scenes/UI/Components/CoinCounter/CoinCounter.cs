using Godot;
using GodsOfTheDungeon.Autoloads;

public partial class CoinCounter : Label
{
    public override void _Ready()
    {
        // Subscribe to GameManager signal when implemented
        GameManager.Instance.CoinsChanged += OnCoinsChanged;
        UpdateDisplay(0);
    }

    private void OnCoinsChanged(int total)
    {
        UpdateDisplay(total);
    }

    private void UpdateDisplay(int coins)
    {
        Text = $"Coins: {coins}";
    }
}
