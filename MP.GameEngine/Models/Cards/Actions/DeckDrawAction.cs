using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// Draws (and resolves) a card from another deck — the "or take a Chance" half of the
/// pay-or-draw cards. Resolved by recursing <c>CardService.DrawCard</c> for <see cref="Deck"/>.
/// See cards-design.md §3 (Card).
/// </summary>
public sealed class DeckDrawAction : CardAction
{
    /// <summary>The deck to draw the follow-on card from (e.g. Chance, PercentageChance).</summary>
    public CardType Deck { get; set; }
}