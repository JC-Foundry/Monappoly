using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A title change driven by a card — returning a property to the bank or handing one into Free
/// Parking, over the target player(s). Each targeted player chooses which of their (tradable)
/// properties via a <c>TargetPropertyPrompt</c>; a player with no eligible property is a silent
/// no-op. Resolved against <c>PropertyTransferService</c>. See <c>design-docs/cards-design.md</c>
/// §3 (Property).
/// </summary>
public sealed class PropertyAction : CardAction
{
    public PropertyActionKind Kind { get; set; }

    /// <summary>Whose property moves.</summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;

    /// <summary>How many properties each targeted player gives up / takes (ignored when <see cref="Set"/> is true).</summary>
    public ushort Count { get; set; } = 1;

    /// <summary>
    /// When true, the action operates on a whole owned <b>set</b> rather than <see cref="Count"/>
    /// individual properties (e.g. "return a set to the bank"). The player picks which complete set.
    /// </summary>
    public bool Set { get; set; }
}