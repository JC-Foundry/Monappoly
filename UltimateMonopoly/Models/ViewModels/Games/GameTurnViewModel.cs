using UltimateMonopoly.Models.DataModels.Games;

namespace UltimateMonopoly.Models.ViewModels.Games;

public class GameTurnViewModel
{
    public string Id { get; }
    public string GameId { get; }
    
    //Whos turn is it
    public string UserId { get; }
    public bool IsFinalTurn { get; }
    public uint TurnNumber { get; }
    
    public string TurnDate { get; }

    public GameTurnViewModel(GameTurn turn)
    {
        Id = turn.Id;
        GameId = turn.GameId;
        UserId = turn.UserId;
        IsFinalTurn = turn.IsFinalTurn;
        TurnNumber = turn.TurnNumber;
        TurnDate = turn.TurnDateUtc.ToLocalTime().ToString("HH:mm:ss dd/MM/yy");
    }
}