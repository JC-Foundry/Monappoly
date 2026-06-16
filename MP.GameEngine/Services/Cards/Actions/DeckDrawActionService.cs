using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.Cards.Actions;

/// <summary>
/// Resolves a card <see cref="DeckDrawAction"/> — drawing (and resolving) a follow-on card from
/// another deck by recursing <see cref="Services.Cards.CardService.DrawCard"/>. Reached via the
/// engine bundle (<c>engine.CardService</c>), so there is no constructor cycle. See cards-design.md
/// §3 (Card).
/// </summary>
public class DeckDrawActionService : ICardActionService<DeckDrawAction>
{
    /// <summary>Draws and resolves a card from the action's deck; the returned suppress flag is irrelevant for a card-driven draw.</summary>
    public async Task<bool> ResolveActionAsync(Framework.GameEngine engine, PlayerModel player, DeckDrawAction action, CancellationToken ct, CardActionContext? context = null)
    {
        _ = await engine.CardService.DrawCard(engine, player, action.Deck, ct);
        return true;
    }
}