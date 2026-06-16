namespace MP.GameEngine.Enums.Cards;

/// <summary>
/// What a <c>DiceAction</c> does — the two dice/triple-bonus families (cards-design.md §3 Dice):
/// manipulating a triple-bonus <i>payout</i>, or converting the effective roll type. The bonus
/// <b>accumulator</b> (+£500 per triple) is unaffected by either — only the payout / roll type is.
/// </summary>
public enum DiceKind
{
    /// <summary>
    /// Modify the triple-bonus <i>payout</i> — scale it (×2 / ×a one-die roll), suppress it (×0,
    /// "you do not receive"/"cancel a player's"), or redirect it to the dice-off lowest roller. The
    /// accumulator still increments. Driven by the payout parameters on the action.
    /// </summary>
    ModifyTripleBonus,

    /// <summary>The double becomes a triple ("convert a double into a triple"). Applied before the row counters update.</summary>
    ConvertDoubleToTriple,

    /// <summary>The triple becomes a double ("your next triple is downgraded to a double"). Applied before the row counters update.</summary>
    DowngradeTripleToDouble
}