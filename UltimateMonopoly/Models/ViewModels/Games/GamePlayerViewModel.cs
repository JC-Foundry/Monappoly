using UltimateMonopoly.Models.DataModels.Games;

namespace UltimateMonopoly.Models.ViewModels.Games;

public class GamePlayerViewModel
{
    public string GameId { get; }
    public string UserId { get; }
    
    public ushort OrderId { get; }
    public bool? IsHost { get; }
    
    public ushort? Dice1 { get; }
    public ushort? Dice2 { get; }
    public ushort? CombinedDice => Dice1.HasValue && Dice2.HasValue ? (ushort)(Dice1.Value + Dice2.Value) : null;

    public GamePlayerViewModel(GamePlayer player, bool? isHost = null)
    {
        GameId = player.GameId;
        UserId = player.UserId;
        OrderId = player.OrderId;
        IsHost = isHost;
        Dice1 = player.Dice1;
        Dice2 = player.Dice2;
    }
}