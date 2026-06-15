namespace MP.GameEngine.Enums.Cards;

/// <summary>What a <c>TurnsAction</c> does to a player's turn counters.</summary>
public enum TurnsKind
{
    /// <summary>Add to <c>PlayerModel.TurnsToMiss</c> (skip N upcoming turns).</summary>
    MissTurns,
    /// <summary>Add to <c>PlayerModel.ExtraTurns</c> (take N extra turns).</summary>
    ExtraTurns
}