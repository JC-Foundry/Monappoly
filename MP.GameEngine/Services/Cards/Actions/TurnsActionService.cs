using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Helpers.Cards;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.Cards.Actions;

/// <summary>
/// Resolves a card <see cref="TurnsAction"/> — adding to each targeted player's
/// <see cref="PlayerModel.TurnsToMiss"/> or <see cref="PlayerModel.ExtraTurns"/>. See
/// cards-design.md §3 (Turns).
/// </summary>
public class TurnsActionService : ICardActionService<TurnsAction>
{
    /// <summary>Adds the missed/extra turns to each targeted player.</summary>
    public async Task ResolveActionAsync(Framework.GameEngine engine, PlayerModel player, TurnsAction action, CancellationToken ct)
    {
        var targets = await CardActionHelper.ResolveTargets(engine, player, action.Target, ct);
        foreach (var target in targets)
        {
            if (action.Kind == TurnsKind.MissTurns)
                target.TurnsToMiss += action.Turns;
            else
                target.ExtraTurns += action.Turns;
        }
    }
}