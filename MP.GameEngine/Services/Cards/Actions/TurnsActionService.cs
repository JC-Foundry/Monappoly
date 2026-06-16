using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Helpers.Cards;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Snapshot;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Services.Cards.Actions;

/// <summary>
/// Resolves a card <see cref="TurnsAction"/> — adding to each targeted player's
/// <see cref="PlayerModel.TurnsToMiss"/> or <see cref="PlayerModel.ExtraTurns"/>. A dice-off action
/// (<see cref="TurnsAction.DiceOff"/>) instead applies the turns to the dice-off winner — e.g.
/// "make the player rolling the lowest miss 3 turns". See cards-design.md §3 (Turns).
/// </summary>
public class TurnsActionService : ICardActionService<TurnsAction>
{
    private readonly DiceService _diceService;

    /// <summary>Creates the turns-action handler over the dice seam its dice-off variant rolls through.</summary>
    public TurnsActionService(DiceService diceService)
    {
        _diceService = diceService;
    }

    /// <summary>Adds the missed/extra turns to the dice-off winner, or to each resolved target.</summary>
    public async Task<bool> ResolveActionAsync(Framework.GameEngine engine, PlayerModel player, TurnsAction action, CancellationToken ct, CardActionContext? context = null)
    {
        // A dice-off card targets the rolling winner ("the lowest other player misses 3 turns") rather
        // than the usual Target. No winner (empty pool) → no-op.
        if (action.DiceOff is not null)
        {
            var winner = await _diceService.ResolveDiceOffTarget(engine, player, action.DiceOff, ct);
            if (winner is not null)
                ApplyTurns(winner, action);
            return true;
        }

        foreach (var target in await CardActionHelper.ResolveTargets(engine, player, action.Target, ct))
            ApplyTurns(target, action);

        return true;
    }

    /// <summary>Adds the action's turns to the player as missed or extra turns.</summary>
    private static void ApplyTurns(PlayerModel target, TurnsAction action)
    {
        if (action.Kind == TurnsKind.MissTurns)
            target.TurnsToMiss += action.Turns;
        else
            target.ExtraTurns += action.Turns;
    }
}