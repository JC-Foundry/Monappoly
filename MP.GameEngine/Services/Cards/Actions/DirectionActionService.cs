using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Helpers.Cards;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.Cards.Actions;

/// <summary>
/// Resolves a card <see cref="DirectionAction"/> — flipping each targeted player's direction of
/// travel via <see cref="PlayerModel.FlipDirection"/> (which no-ops, and cites the rule, for a
/// player who has not yet passed GO). See cards-design.md §3 (Direction).
/// </summary>
public class DirectionActionService : ICardActionService<DirectionAction>
{
    /// <summary>Flips the direction of each targeted player.</summary>
    public async Task<bool> ResolveActionAsync(Framework.GameEngine engine, PlayerModel player, DirectionAction action, CancellationToken ct, CardActionContext? context = null)
    {
        var targets = await CardActionHelper.ResolveTargets(engine, player, action.Target, ct);
        foreach (var target in targets)
            target.FlipDirection(engine);

        return true;
    }
}