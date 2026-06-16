using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A dice / triple-bonus effect driven by a card (cards-design.md §3 Dice). One action covers the
/// whole family via <see cref="Kind"/>:
/// <list type="bullet">
/// <item><see cref="DiceKind.ModifyTripleBonus"/> — change the triple-bonus <i>payout</i> for the
/// <see cref="Target"/> player (scale, suppress, or redirect), driven by the payout parameters
/// below. The +£500 accumulator still increments — only the payout is touched.</item>
/// <item><see cref="DiceKind.ConvertDoubleToTriple"/> / <see cref="DiceKind.DowngradeTripleToDouble"/>
/// — flip the effective roll type (applied before the doubles/triples-in-a-row counters update).</item>
/// </list>
/// The payout is realised by the split <c>PlayerService.ResolveTripleBonus</c>; the conversions hook
/// the orchestrator's double/triple branches. See <c>design-docs/cards-dev-changes.md</c> §2.13 / §4.
/// </summary>
public sealed class DiceAction : CardAction
{
    public DiceKind Kind { get; set; }

    /// <summary>
    /// Who the effect acts on — the holder (default), or a chosen player ("cancel a player's triple
    /// bonus"). For the roll-type conversions this is the roller, i.e. the holder.
    /// </summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;

    // ── ModifyTripleBonus payout parameters (exactly one applies; ignored for the conversions) ──

    /// <summary>
    /// A fixed payout factor: <c>0</c> suppresses the payout ("you do not receive" / "cancel a
    /// player's triple bonus"), <c>2</c> doubles it. Null when the payout is dice- or redirect-driven.
    /// </summary>
    public ushort? PayoutFactor { get; set; }

    /// <summary>When true, the triple-bonus payout is multiplied by a fresh one-die roll ("multiply your triple bonus by the number rolled").</summary>
    public bool PayoutMultiplyByDie { get; set; }

    /// <summary>When true, the triple-bonus payout is redirected (via a one-die dice-off) to the lowest roller ("the player with the lowest roll receives your triple bonus").</summary>
    public bool PayoutRedirectToLowestRoller { get; set; }
}