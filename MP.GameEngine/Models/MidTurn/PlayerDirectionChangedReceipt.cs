using MP.GameEngine.Enums.Players;

namespace MP.GameEngine.Models.MidTurn;

public class PlayerDirectionChangedReceipt : EventReceipt
{
    public PlayerDirection InitialDirection { get; set; }
    public PlayerDirection FinalDirection { get; set; }
}