namespace MP.GameEngine.Models.MidTurn;

public class PlayerLeftJailReceipt : EventReceipt
{
    public ushort BoardIndex { get; set; }
    public uint JailCost { get; set; }
}