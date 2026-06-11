namespace MP.GameEngine.Enums.Cards;

/// <summary>Where a card's money comes from / goes to.</summary>
public enum MoneyCounterparty
{
    Bank,
    FreeParking,
    /// <summary>Every other (non-bankrupt) player pays/receives the amount.</summary>
    EachPlayer,
    /// <summary>The player who rolls highest on a single die (resolved by a dice-off).</summary>
    HighestRoller,
    /// <summary>The player who rolls lowest on a single die (resolved by a dice-off).</summary>
    LowestRoller
}