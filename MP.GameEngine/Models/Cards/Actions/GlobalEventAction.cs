using MP.GameEngine.Enums;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// Starts a table-wide global event driven by a card (cards-design.md §7 / §3 Global), routed to
/// <c>GlobalEventService</c> — it sets <c>GameModel.GlobalEventInfo</c>, which the rent / tax /
/// Free Parking / jail read-hooks already honour. The event clears when any player next rolls a
/// double (the built clear-on-double in <c>PlayerTurnOrchestrator</c>).
/// </summary>
public sealed class GlobalEventAction : CardAction
{
    /// <summary>Which global event to start.</summary>
    public GlobalEvent Event { get; set; }

    /// <summary>The multiplier for the rent / tax events (e.g. utilities ×10, stations ×0, tax ×2). Null for the flag events.</summary>
    public ushort? Multiplier { get; set; }
}