using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A money movement driven by a card, resolved by the card interpreter against
/// <c>TransactionService</c>. Covers the bulk of the Money inventory; the
/// highest/lowest-roller counterparties require a one-die dice-off to resolve the
/// target player. See <c>design-docs/cards-design.md</c> §3 / <c>cards-actions.md</c>.
/// </summary>
public sealed class MoneyAction : CardAction
{
    /// <summary>Base amount before per-unit / dice / percentage scaling. Read as a <i>percent</i> for the percentage <see cref="Basis"/> values.</summary>
    public long Amount { get; set; }

    public MoneyDirection Direction { get; set; }
    public MoneyCounterparty Counterparty { get; set; }

    /// <summary>
    /// Whose money moves. <see cref="PlayerTarget.Self"/> (default) = the holder, with
    /// <see cref="Counterparty"/> driving where it flows (the original behaviour). Any other value =
    /// each targeted player is the subject of a Bank / Free Parking move ("each player receives
    /// £1000 from the bank").
    /// </summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;

    /// <summary>Where the amount is derived from (a fixed figure, a fraction of cash / the FP pot, the triple bonus, …).</summary>
    public MoneyAmountBasis Basis { get; set; } = MoneyAmountBasis.Fixed;

    /// <summary>
    /// When true, swaps the holder's entire cash balance with the counterparty — a chosen player,
    /// or the highest/lowest dice-off roller (per <see cref="Counterparty"/>). Ignores the amount.
    /// </summary>
    public bool SwapCash { get; set; }

    /// <summary>Multiplies <see cref="Amount"/> by the player's houses / hotels / properties.</summary>
    public MoneyPerUnit PerUnit { get; set; }

    /// <summary>Multiplies the amount by a dice roll (e.g. "£200 × 1 die").</summary>
    public DiceMultiplier DiceMultiplier { get; set; }

    /// <summary>Percentage cards: the realised amount scales by the player's %cap (100 / 50 / 10).</summary>
    public bool PercentageApplies { get; set; }

    /// <summary>
    /// For a <see cref="MoneyCounterparty.HighestRoller"/> / <see cref="MoneyCounterparty.LowestRoller"/>
    /// dice-off, whether the card holder also rolls (and can win). When the holder wins their own
    /// dice-off the movement is with themselves — i.e. a no-op. Default false: only the other players
    /// roll. Ignored for every other counterparty.
    /// </summary>
    public bool IncludeHolderInRoll { get; set; }
}