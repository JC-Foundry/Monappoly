using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Conditions;

public class CardCondition
{
    //TODO: make a GUID helper, so re-imported cards get same GUID as card in play
    public string ConditionId { get; set; }
    
    public CardTrigger Trigger { get; set; }
}