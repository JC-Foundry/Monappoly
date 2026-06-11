namespace MP.GameEngine.Enums.Cards;

/// <summary>The kind of board movement a <c>MovementAction</c> performs.</summary>
public enum MovementKind
{
    /// <summary>Move a signed number of spaces (+ forward in direction of travel, - back).</summary>
    MoveSpaces,
    /// <summary>Advance to a specific board index.</summary>
    AdvanceToIndex,
    /// <summary>Advance to the nearest space of a kind (see <c>NearestKind</c>).</summary>
    AdvanceToNearest,
    /// <summary>Swap board positions with another player — no landed-space action (game-rules.md Movement rule 4).</summary>
    Swap,
    /// <summary>Move to Just Visiting (not jail).</summary>
    GoToJustVisiting
}