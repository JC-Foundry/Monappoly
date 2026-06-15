using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A direction change driven by a card, resolved via <c>PlayerModel.FlipDirection</c>. Jailed
/// players are included where targeted (the flip applies when they next move). See
/// <c>design-docs/cards-design.md</c> §3 (Direction).
/// </summary>
public sealed class DirectionAction : CardAction
{
    /// <summary>Who flips direction (self, every other player, a chosen player, or everyone).</summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;
}