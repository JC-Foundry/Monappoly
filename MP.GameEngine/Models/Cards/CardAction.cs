using System.Text.Json.Serialization;
using MP.GameEngine.Models.Cards.Actions;

namespace MP.GameEngine.Models.Cards;

// Cards (and their actions) persist in the game snapshot, so actions serialise
// polymorphically by discriminator — same pattern as EventReceipt / Prompt. Each
// new action type adds a [JsonDerivedType] line.
[JsonPolymorphic]
[JsonDerivedType(typeof(MoneyAction), "Money")]
[JsonDerivedType(typeof(MovementAction), "Movement")]
[JsonDerivedType(typeof(JailAction), "Jail")]
public abstract class CardAction
{
    //TODO: make a GUID helper, so re-imported cards get same GUID as card in play
    public string ActionId { get; set; }
}