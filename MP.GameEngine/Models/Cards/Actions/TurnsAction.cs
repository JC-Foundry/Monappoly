using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A turn-counter change driven by a card, resolved against the target(s)' <c>TurnsToMiss</c> /
/// <c>ExtraTurns</c>. See <c>design-docs/cards-design.md</c> §3 (Turns).
/// </summary>
public sealed class TurnsAction : CardAction
{
    public TurnsKind Kind { get; set; }

    /// <summary>How many turns to add (missed or extra).</summary>
    public ushort Turns { get; set; }

    /// <summary>Who it acts on.</summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;

    /// <summary>
    /// When set, the turns are applied to a dice-off winner instead of <see cref="Target"/> — e.g.
    /// "make the player rolling the lowest miss 3 turns" (<c>DiceOff{ Highest=false, IncludeHolder=false }</c>).
    /// </summary>
    public DiceOff? DiceOff { get; set; }
}