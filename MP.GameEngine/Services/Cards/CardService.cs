using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Models.Cards;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.EventReceipts;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Snapshot;
using MP.GameEngine.Models.Snapshot.Cards;
using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Players;
using MP.GameEngine.Helpers;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Services.Cards;

/// <summary>
/// The card interpreter — draws cards from the per-type decks and resolves their
/// data-driven actions against the existing engine services. The card model is pure
/// data; all behaviour lives here (cards-design.md §3). Action handlers are dispatched
/// through one <see cref="ApplyAction"/> switch.
/// </summary>
public class CardService
{
    private readonly TransactionService _transactionService;
    private readonly MovementService _movementService;
    private readonly BoardService _boardService;
    private readonly JailService _jailService;

    public CardService(TransactionService transactionService,
        MovementService movementService,
        BoardService boardService,
        JailService jailService)
    {
        _transactionService = transactionService;
        _movementService = movementService;
        _boardService = boardService;
        _jailService = jailService;
    }


    /// <summary>
    /// Draws the next card of <paramref name="type"/> for <paramref name="player"/> and
    /// either resolves it immediately (resolve-on-draw, <see cref="CardConditionType.None"/>)
    /// or adds it to the player's hand (keep-until-needed). Resolved cards return to the back
    /// of the deck. No-op on an empty deck. See cards-design.md §4 (interaction modes), §9 (decks).
    /// </summary>
    public async Task DrawCard(Framework.GameEngine engine, PlayerModel player, CardType type, CancellationToken ct)
    {
        var card = engine.Cache.Game.CardDecks.Take(type);
        if (card is null)
            //Empty deck — nothing to draw.
            return;

        if (!card.IsKeepUntilNeeded)
        {
            //Resolve-on-draw (override-on-draw, §4b): apply now, then return to the deck.
            await ResolveCard(engine, player, card, ct);
            engine.Cache.Game.CardDecks.HandBack(type, card);
            return;
        }

        //Keep-until-needed — held in the player's hand until its trigger fires (held-card
        //trigger evaluation is a later increment).
        player.Cards.Add(card);
        engine.EventEmitter.Emit(new CardTakenReceipt { PlayerId = player.PlayerId, CardType = card.CardType });
    }


    /// <summary>
    /// Resolves a card: selects the group to apply (a single group applies directly; multiple
    /// groups are an OR-choice surfaced via <see cref="CardOptionPrompt"/>), then applies that
    /// group's actions in order (ANDed). Emits a <see cref="CardPlayedReceipt"/> — a resolve-on-draw
    /// card still counts as played even though it never reaches the hand.
    /// </summary>
    public async Task ResolveCard(Framework.GameEngine engine, PlayerModel player, CardModel card, CancellationToken ct)
    {
        if (card.Groups.Count == 0)
            //Nothing to apply.
            return;

        _ = await engine.PromptProvider.Acknowledge(player.PlayerId, $"{card.CardType.ToDisplayName()} Card",
            card.CardText, timeout: TimeSpan.FromSeconds(30), ct: ct);
        
        CardGroup group;
        if (card.Groups.Count == 1)
        {
            //Single group — no choice to make.
            group = card.Groups[0];
        }
        else
        {
            //Multiple groups = a choice (cards-design.md §2). Options are keyed by the stable
            //GroupId (keys-not-indexes), labelled with the group's text.
            var response = await engine.PromptProvider.RequestAsync(new CardOptionPrompt
            {
                PlayerId = player.PlayerId,
                Title = "Choose an option",
                Body = card.CardText,
                Options = card.Groups.Select(g => new CardOption(g.GroupId, g.GroupText)).ToList()
            }, ct);

            group = card.Groups.First(g => g.GroupId == response.SelectedKey);
        }

        //Actions within the chosen group are ANDed — applied in order.
        foreach (var action in group.Actions)
            await ApplyAction(engine, player, action, ct);

        engine.EventEmitter.Emit(new CardPlayedReceipt { PlayerId = player.PlayerId, CardType = card.CardType });
    }


    /// <summary>
    /// Dispatches a single <see cref="CardAction"/> to its handler — the interpreter seam.
    /// Each action type is a thin dispatch to the relevant engine service (cards-design.md §3),
    /// some with bespoke logic (the dice-off, the nearest-finder). New action types add a branch.
    /// </summary>
    private Task ApplyAction(Framework.GameEngine engine, PlayerModel player, CardAction action, CancellationToken ct)
        => action switch
        {
            MoneyAction m    => ApplyMoney(engine, player, m, ct),
            MovementAction v => ApplyMovement(engine, player, v, ct),
            JailAction j     => ApplyJail(engine, player, j, ct),
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, "Unhandled card action type.")
        };


    // ── Action handlers ──────────────────────────────────────────────────
    // Each is a thin dispatch to the relevant engine service; the bespoke bits
    // (target resolution, the nearest-finder, the swap) stay local.

    private async Task ApplyMoney(Framework.GameEngine engine, PlayerModel player, MoneyAction action, CancellationToken ct)
    {
        // TODO: Deferred (per decisions): the EachPlayer loop (payer-POV per transactions.md §4) and the
        // Highest/LowestRoller dice-off (DiceService) aren't wired yet.
        if (action.Counterparty is MoneyCounterparty.EachPlayer
            or MoneyCounterparty.HighestRoller or MoneyCounterparty.LowestRoller)
            return;

        var amount = RealiseAmount(engine, player, action);
        if (amount == 0)
            return;

        var counterparty = action.Counterparty == MoneyCounterparty.FreeParking
            ? TransactionCounterparty.FreeParking
            : TransactionCounterparty.Bank;

        if (action.Direction == MoneyDirection.Receive)
            await _transactionService.ReceiveCardPayout(engine, player, amount, counterparty, null, ct);
        else
            await _transactionService.PayCardCharge(engine, player, amount, counterparty, null, ct);
    }

    /// <summary>
    /// Base amount scaled by the per-unit count (houses/hotels/properties owned) and, for percentage
    /// cards, the player's %cap (100/50/10). The dice multiplier is a deferred DiceService roll.
    /// </summary>
    private static uint RealiseAmount(Framework.GameEngine engine, PlayerModel player, MoneyAction action)
    {
        long amount = action.Amount;

        var (houses, hotels) = engine.Cache.Game.GetHousesAndHotelsTaken(player.PlayerId);
        amount *= action.PerUnit switch
        {
            MoneyPerUnit.PerHouse => houses,
            MoneyPerUnit.PerHotel => hotels,
            MoneyPerUnit.PerProperty => engine.Cache.Game.GetOwnedProperties(player.PlayerId).Count,
            _ => 1
        };

        // TODO: DiceMultiplier — a fresh roll via DiceService (deferred, decision #1).

        if (action.PercentageApplies)
            amount = (amount * engine.Cache.Game.PlayerPercentCap(player.PlayerId)) / 100;

        return (uint)MoneyHelper.NormaliseAmount(Math.Max(0, amount), engine.Cache.RoundingRule, 
            action.Direction == MoneyDirection.Receive 
                ? FinancialReason.CardPayout 
                : FinancialReason.CardCharge);
    }


    private async Task ApplyMovement(Framework.GameEngine engine, PlayerModel player, MovementAction action, CancellationToken ct)
    {
        if (action.Kind == MovementKind.Swap)
        {
            await ApplySwap(engine, player, action, ct);
            return;
        }

        var targets = await ResolveTargets(engine, player, action.Target, ct);
        foreach (var target in targets)
        {
            //TODO: Respect CollectGoBonus (deferred).
            switch (action.Kind)
            {
                case MovementKind.MoveSpaces:
                    await _movementService.MovePlayer(engine, target, action.Spaces, ct);
                    break;
                case MovementKind.AdvanceToIndex when action.TargetIndex is { } index:
                    await _movementService.AdvancePlayer(engine, target, index, MovementDirection(action), ct);
                    break;
                case MovementKind.AdvanceToNearest:
                    await _movementService.AdvancePlayer(engine, target, FindNearest(target, action.Nearest), MovementDirection(action), ct);
                    break;
                case MovementKind.GoToJustVisiting:
                    await _movementService.AdvancePlayer(engine, target, IndexHelper.JustVisitingSpace,
                        PlayerMovementDirection.CounterDirectionOfTravel, ct);
                    continue;   // Just Visiting performs no space action
                default:
                    continue;
            }

            // Perform the landed space's action (rent, GO, tax, ...) unless the card suppresses it.
            if (action.ResolveLandedSpace)
                await _boardService.ResolveBoardSpaceForPlayer(engine, target, ct);
        }
    }

    /// <summary>
    /// A swap exchanges the holder's and one chosen player's board positions — no GO bonus, and no
    /// landed-space action (game-rules.md Movement rule 4).
    /// </summary>
    private async Task ApplySwap(Framework.GameEngine engine, PlayerModel player, MovementAction action, CancellationToken ct)
    {
        var pick = action.Target == PlayerTarget.Self ? PlayerTarget.ChosenPlayer : action.Target;
        var target = (await ResolveTargets(engine, player, pick, ct)).FirstOrDefault();
        if (target is null || target.PlayerId == player.PlayerId)
            return;

        var holderIndex = player.BoardIndex;
        player.BoardIndex = target.BoardIndex;
        target.BoardIndex = holderIndex;

        engine.EventEmitter.Emit(new PlayerSwappedReceipt
        {
            PlayerId = player.PlayerId,
            SwappedPlayerId = target.PlayerId,
            InitialPlayerBoardIndex = holderIndex,
            FinalPlayerBoardIndex = player.BoardIndex
        });
    }

    private static PlayerMovementDirection MovementDirection(MovementAction action)
        => action.CollectGoBonus
            ? PlayerMovementDirection.DirectionOfTravel
            : PlayerMovementDirection.CounterDirectionOfTravel;

    private static ushort FindNearest(PlayerModel player, NearestKind kind)
    {
        var targets = kind switch
        {
            NearestKind.Station => PropertySetHelper.StationIndexes,
            NearestKind.Utility => PropertySetHelper.UtilityIndexes,
            _ => IndexHelper.BuildablePropertyIndexes
        };

        var index = player.BoardIndex;
        for (var step = 0; step < IndexHelper.PhysicalBoardSize; step++)
        {
            (index, _) = IndexHelper.MoveIndex(index, 1, player.Direction);
            if (targets.Contains(index))
                return index;
        }
        return player.BoardIndex;   // fallback — a board always holds the target kind
    }


    private async Task ApplyJail(Framework.GameEngine engine, PlayerModel player, JailAction action, CancellationToken ct)
    {
        var targets = await ResolveTargets(engine, player, action.Target, ct);
        foreach (var target in targets)
        {
            switch (action.Kind)
            {
                case JailKind.SendToJail:
                    if (action.TurnsOverride is { } turns)
                        target.MaxJailTurnsOverride = turns;
                    await _jailService.SendPlayerToJail(engine, target, ct);
                    break;
                case JailKind.Release when target.IsInJail:
                    await _jailService.LeaveJailByCard(engine, target, ct);
                    break;
            }
        }
    }


    // ── Shared helpers ───────────────────────────────────────────────────

    /// <summary>
    /// Resolves a <see cref="PlayerTarget"/> to the players an action acts on — the holder, a
    /// chosen player (via <see cref="TargetPlayerPrompt"/>), or every active other player.
    /// </summary>
    private async Task<List<PlayerModel>> ResolveTargets(Framework.GameEngine engine, PlayerModel holder,
        PlayerTarget target, CancellationToken ct)
    {
        switch (target)
        {
            case PlayerTarget.Self:
                return [holder];
            case PlayerTarget.AllOthers:
                return engine.Cache.Game.GetPlayers(holder.PlayerId);
            case PlayerTarget.ChosenPlayer:
                var others = engine.Cache.Game.GetPlayers(holder.PlayerId);
                if (others.Count == 0)
                    return [];

                var response = await engine.PromptProvider.RequestAsync(new TargetPlayerPrompt
                {
                    PlayerId = holder.PlayerId,
                    Title = "Choose a Player",
                    Body = "Select a player.",
                    EligiblePlayerIds = others.Select(p => p.PlayerId).ToList(),
                    Count = 1
                }, ct);

                return response.SelectedPlayerIds
                    .Select(id => engine.Cache.Game.GetPlayer(id))
                    .Where(p => p is not null)
                    .ToList()!;
            default:
                throw new ArgumentOutOfRangeException(nameof(target), target, null);
        }
    }
}