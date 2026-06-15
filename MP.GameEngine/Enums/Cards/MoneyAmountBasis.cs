namespace MP.GameEngine.Enums.Cards;

/// <summary>
/// Where a card money amount is derived from. For the percentage bases, the action's
/// <c>Amount</c> is read as the <i>percent</i> (e.g. 50 = half, 100 = all).
/// </summary>
public enum MoneyAmountBasis
{
    /// <summary><c>Amount</c> is the literal figure (the default).</summary>
    Fixed,
    /// <summary><c>Amount</c>% of the subject's current cash ("hand back half of your money").</summary>
    PercentOfOwnCash,
    /// <summary><c>Amount</c>% of the Free Parking pot ("receive all"/"50% of the amount in free parking").</summary>
    PercentOfFreeParkingPot,
    /// <summary>The subject's current triple bonus (<c>PlayerModel.TripleBonus</c>).</summary>
    TripleBonus,
    /// <summary>The snake-eyes (double-1) bonus (<c>RuleDictionary.SnakeEyesBonus</c>).</summary>
    SnakeEyesBonus
}
