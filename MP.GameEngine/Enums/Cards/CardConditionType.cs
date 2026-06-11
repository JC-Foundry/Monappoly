namespace MP.GameEngine.Enums.Cards;

/// <summary>
/// How a card is engaged once it is held — the orthogonal axis to
/// <see cref="CardTrigger"/> (which event makes a held card live). The two
/// dimensions are forced-vs-choice and cardholder's-turn-vs-any-player's-turn.
/// See <c>design-docs/cards-design.md</c> §5.
/// </summary>
public enum CardConditionType
{
    /// <summary>
    /// Resolve immediately on draw; not kept. The card's only "trigger" is being
    /// drawn from its <see cref="CardType"/> deck — the override-on-draw path
    /// (<c>cards-design.md</c> §4b). Carries no <see cref="CardTrigger"/>.
    /// </summary>
    None,

    /// <summary>Forced: played automatically when its trigger fires on the holder's own turn.</summary>
    MetCardholderTurn,

    /// <summary>Forced: played automatically when its trigger fires on any player's turn.</summary>
    MetAnyPlayerTurn,

    /// <summary>Optional: the holder may play it when its trigger fires on their own turn.</summary>
    ChoiceCardholderTurn,

    /// <summary>Optional: the holder may play it when its trigger fires on any player's turn.</summary>
    ChoiceAnyPlayerTurn
}