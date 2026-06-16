namespace MP.GameEngine.Enums.Cards;

/// <summary>
/// Where a <c>MoneyAction</c>'s base figure comes from. The fourth amount source in the same family
/// as <c>PercentageApplies</c> / <c>DiceMultiplier</c> / <c>PerUnit</c> (all derive the amount from
/// engine context) — but this one reads the amount the <i>trigger</i> supplied. See
/// <c>design-docs/card-triggers.md</c> §6.
/// </summary>
public enum AmountSource
{
    /// <summary>The base comes from the action itself — <c>Amount</c> / <c>Basis</c> (the default).</summary>
    Fixed,
    /// <summary>
    /// The base is the amount the firing trigger supplied (the tax just assessed, the Free Parking
    /// take, the payment about to be made, …), carried on the <c>CardActionContext</c>. With this
    /// source <c>Amount</c> is reused as the <b>factor</b> ("double tax" = ×2, "exactly that" = ×1).
    /// </summary>
    TriggerAmount
}