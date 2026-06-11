namespace MP.GameEngine.Models.Cards;

public class CardGroup
{
    //TODO: make a GUID helper, so re-imported cards get same GUID as card in play
    public string GroupId { get; set; }
    public string GroupText { get; set; }
    
    public IReadOnlyList<CardAction> Actions { get; set; }
}