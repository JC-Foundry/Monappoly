using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Enums;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Snapshot;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Services.Cards.Actions;

/// <summary>
/// Resolves a card <see cref="GlobalEventAction"/> — starting the matching table-wide event via
/// <see cref="GlobalEventService"/>. The store, the rent/tax/FP/jail read-hooks, and the
/// clear-on-double are all already built; this action is the missing "set it from a card" seam.
/// See cards-design.md §7.
/// </summary>
public class GlobalEventActionService : ICardActionService<GlobalEventAction>
{
    private readonly GlobalEventService _globalEventService;

    /// <summary>Creates the global-event-action handler over the global-event seam.</summary>
    public GlobalEventActionService(GlobalEventService globalEventService)
    {
        _globalEventService = globalEventService;
    }

    /// <summary>Starts the action's global event.</summary>
    public Task ResolveActionAsync(Framework.GameEngine engine, PlayerModel player, GlobalEventAction action, CancellationToken ct)
    {
        switch (action.Event)
        {
            case GlobalEvent.StationRent:
                _globalEventService.StartStationRentEvent(engine, action.Multiplier);
                break;
            case GlobalEvent.UtilityRent:
                _globalEventService.StartUtilityRentEvent(engine, action.Multiplier);
                break;
            case GlobalEvent.TaxMultiplier:
                _globalEventService.StartTaxMultiplierEvent(engine, action.Multiplier);
                break;
            case GlobalEvent.RealFreeParking:
                _globalEventService.StartRealFreeParkingEvent(engine);
                break;
            case GlobalEvent.JailFull:
                _globalEventService.StartJailFullEvent(engine);
                break;
        }

        return Task.CompletedTask;
    }
}