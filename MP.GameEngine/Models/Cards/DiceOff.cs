namespace MP.GameEngine.Models.Cards;

/// <summary>
/// Configures a dice-off — the generic "the player who rolls the highest/lowest" picker, reusable by
/// <i>any</i> card action that targets (or pays/receives from) a dice-off winner: a money
/// counterparty, a turns target, a swap partner, a card recipient, the triple-bonus redirect, … It is
/// deliberately not tied to one action type; <c>DiceService.ResolveDiceOffTarget</c> resolves it to
/// the winning player over the shared <c>RollDiceOff</c> primitive.
/// </summary>
public sealed class DiceOff
{
    /// <summary>How many dice each candidate rolls (1 or 2). Default 1.</summary>
    public ushort DiceCount { get; set; } = 1;

    /// <summary>True = the highest total wins; false (default) = the lowest.</summary>
    public bool Highest { get; set; }

    /// <summary>
    /// Whether the card holder joins the roll. <c>true</c> = "each player rolls" (the holder can win);
    /// <c>false</c> (default) = only the other players roll (the holder targeting someone else).
    /// </summary>
    public bool IncludeHolder { get; set; }
}