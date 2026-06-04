namespace MP.GameEngine.Enums;

/// <summary>
/// Outcome of a shortfall-prompt round. Dictates whether the outer
/// transaction should apply (FundsRaised) or be abandoned (DebtSettled,
/// Bankrupted).
/// </summary>
public enum ShortfallOutcome
{
    /// <summary>Loan / mortgage / sell-building gave the player enough cash; outer transaction continues.</summary>
    FundsRaised,

    /// <summary>A creditor deal discharged the debt; outer transaction must not also apply.</summary>
    DebtSettled,

    /// <summary>Player declared bankruptcy; outer transaction stops here.</summary>
    Bankrupted
}