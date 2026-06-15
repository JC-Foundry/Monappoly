namespace MP.GameEngine.Enums.Cards;

/// <summary>What a <c>LoansAction</c> does to a player's outstanding loans.</summary>
public enum LoanCardKind
{
    /// <summary>Forgive every outstanding loan — the debt vanishes, no money is paid.</summary>
    WipeAll,
    /// <summary>Repay every outstanding loan in full — the player pays the bank (shortfall allowed).</summary>
    RepayAll,
    /// <summary>
    /// Wipe every player's loans, then reward the players who were already loan-free (snapshotted
    /// before the wipe) with £1000 each and a forced property return to the bank ("all outstanding
    /// loans are wiped out … any player with no loans receives £1000 but must return a property").
    /// </summary>
    WipeAllAndRewardClear
}