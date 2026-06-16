namespace MP.GameEngine.Enums.Cards;

/// <summary>Where a card's money comes from / goes to.</summary>
public enum MoneyCounterparty
{
    Bank,
    FreeParking,
    /// <summary>Every other (non-bankrupt) player pays/receives the amount.</summary>
    EachPlayer,
    /// <summary>A dice-off winner — configured by the action's <c>DiceOff</c> (highest/lowest, pool) — pays/receives the amount.</summary>
    DiceOffPlayer
}