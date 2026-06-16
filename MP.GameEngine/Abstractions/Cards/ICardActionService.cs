using MP.GameEngine.Models.Cards;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Abstractions.Cards;

/// <summary>
/// Handler for one concrete <see cref="CardAction"/> type — the per-action seam
/// <see cref="Services.Cards.CardService"/> dispatches to. One implementation per action type
/// (Money / Movement / Jail), each owning that type's behaviour in isolation so the card
/// interpreter stays a thin orchestrator (cards-design.md §3).
/// </summary>
/// <typeparam name="T">The concrete <see cref="CardAction"/> this service resolves.</typeparam>
public interface ICardActionService<T>
    where T : CardAction
{
    /// <summary>
    /// Applies <paramref name="action"/> for <paramref name="player"/> against the engine —
    /// the action's effect (money movement, board movement, jail entry/exit, …). Resolves any
    /// follow-on targeting prompts the action needs.
    /// </summary>
    /// <param name="engine">The game engine bundle the action mutates.</param>
    /// <param name="player">The card holder the action is resolved for.</param>
    /// <param name="action">The action to apply.</param>
    /// <param name="ct">Cancellation token, tripped on game cancellation.</param>
    /// <param name="context">
    /// Optional dynamic context the firing card carried in (e.g. a trigger-supplied amount). Only the
    /// <c>TriggerAmount</c> money source reads it; every other action ignores it. <c>null</c> for
    /// resolve-on-draw and any play with no trigger context.
    /// </param>
    /// <returns>
    /// Whether the action took effect. Almost always <c>true</c> — including silent no-ops (e.g. a
    /// zero-amount charge). <c>false</c> only when the action could not be performed at all and the
    /// played card should therefore be <b>retained in hand</b>, not consumed — currently just a
    /// blocked jail release (a player locked in jail by a card). The card interpreter ANDs the chosen
    /// group's results and uses the outcome to decide whether to consume the card (see <c>PlayCard</c>).
    /// </returns>
    Task<bool> ResolveActionAsync(Services.Framework.GameEngine engine, PlayerModel player, T action, CancellationToken ct, CardActionContext? context = null);
}