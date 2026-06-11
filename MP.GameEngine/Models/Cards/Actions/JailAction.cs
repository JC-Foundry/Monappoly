using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// Jail entry/exit driven by a card, resolved against <c>JailService</c>. Pure board
/// movement (e.g. "go back 3 spaces") is the separate <see cref="MovementAction"/>.
/// See <c>design-docs/cards-design.md</c> §3.
/// </summary>
public sealed class JailAction : CardAction
{
    public JailKind Kind { get; set; }

    /// <summary>Who it acts on (e.g. send self / a chosen player / all players to jail).</summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;

    /// <summary>
    /// Optional jail-term override for a <see cref="JailKind.SendToJail"/> (e.g. "go to jail
    /// for 10 turns" → <c>PlayerModel.MaxJailTurnsOverride</c>). Null = the default limit.
    /// </summary>
    public ushort? TurnsOverride { get; set; }
}