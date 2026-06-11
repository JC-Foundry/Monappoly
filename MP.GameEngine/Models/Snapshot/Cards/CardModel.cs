using System.Text.Json.Serialization;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Models.Cards;
using MP.GameEngine.Models.Cards.Conditions;

namespace MP.GameEngine.Models.Snapshot.Cards;

public class CardModel
{
    public string CardId { get; set; }  //TODO: make a GUID helper, so re-imported cards get same GUID as card in play
    public string CardText { get; set; }
    
    public CardType CardType { get; set; }
    
    public IReadOnlyList<CardGroup> Groups { get; set; } = [];
    public IReadOnlyList<CardCondition> Conditions { get; set; } = [];

    public CardConditionType ConditionType { get; set; } = CardConditionType.None;
    
    [JsonIgnore]
    public bool IsKeepUntilNeeded => ConditionType != CardConditionType.None;
    
    public CardModel()
    {
    }

    public CardModel(CardModel model)
    {
        CardId = model.CardId;
        CardText = model.CardText;
        CardType = model.CardType; 
        ConditionType = model.ConditionType;

        // Groups / Conditions are immutable card-definition data (fixed for the game's
        // life), so the working-copy clone shares the references rather than deep-copying
        // the whole action tree every turn. If a per-instance mutable field is ever added
        // (e.g. a charge counter), deep-copy that field — not this static content.
        Groups = model.Groups;
        Conditions = model.Conditions;
    }
}