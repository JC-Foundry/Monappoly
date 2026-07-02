using Microsoft.Extensions.DependencyInjection;
using MP.GameEngine.Abstractions;
using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Enums.Games;
using MP.GameEngine.Enums.Players;
using MP.GameEngine.Enums.Properties;
using MP.GameEngine.Helpers;
using MP.GameEngine.Helpers.Cards;
using MP.GameEngine.Helpers.RuleSet;
using MP.GameEngine.Models;
using MP.GameEngine.Models.DTOs;
using MP.GameEngine.Models.Snapshot;
using MP.GameEngine.Models.Snapshot.Cards;
using MP.GameEngine.Services;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Harness;

/// <summary>
/// Fluent builder that assembles a game in a <b>mid-game state</b> and hands back a ready-to-drive
/// <see cref="GameScenario"/> (a <see cref="GameCacheModel"/> plus a wired
/// <see cref="MP.GameEngine.Services.Framework.GameEngine"/> and its DI scope).
///
/// <para>This builds <i>state only</i> — players, money, property ownership, board position, the turn
/// counters, and the game-level flags (reserve rule, Free Parking pot, global event). It does <b>not</b>
/// touch cards beyond building the real shuffled decks via <see cref="CardDeckHelper.BuildCardDecks"/>:
/// drawing, playing, keeping and handing back cards is <c>CardService</c>'s job and is exercised by
/// driving the engine, not by the builder.</para>
///
/// <para>The turn always begins at <see cref="TurnState.StartOfTurn"/>; mid-turn phase is cache-only and
/// is reached by driving the orchestrator, not by persisting it. Point-in-turn inputs (roll, buy/decline,
/// card choices) are supplied by answering the engine's prompts through the scenario's prompt provider.</para>
/// </summary>
public sealed class GameScenarioBuilder
{
    private string _gameId = "game-1";
    private string _hostPlayerId = "host";
    private GameRoundingRule _rounding = GameRoundingRule.None;
    private uint _turnNumber = 10;
    private string? _currentPlayerId;
    private bool _reserveRuleActive;
    private uint _freeParkingAmount;
    private EventInfo? _globalEvent;
    private DiceRollType? _modifiedRollType;

    private readonly List<PlayerSpec> _players = [];
    // Cards pinned to the front of a deck so a later in-turn draw yields them deterministically.
    private readonly List<(CardType Type, string Text)> _nextCards = [];
    // Cards seeded into a hand at build time via the real CardService.DrawCard.
    private readonly List<(string PlayerId, CardType Type, string Text)> _heldCards = [];

    public static GameScenarioBuilder Create() => new();

    public GameScenarioBuilder WithGameId(string id) { _gameId = id; return this; }
    public GameScenarioBuilder WithHost(string hostPlayerId) { _hostPlayerId = hostPlayerId; return this; }
    public GameScenarioBuilder WithRounding(GameRoundingRule rounding) { _rounding = rounding; return this; }
    public GameScenarioBuilder WithTurnNumber(uint turnNumber) { _turnNumber = turnNumber; return this; }
    public GameScenarioBuilder WithCurrentPlayer(string playerId) { _currentPlayerId = playerId; return this; }
    public GameScenarioBuilder WithReserveRuleActive(bool active = true) { _reserveRuleActive = active; return this; }
    public GameScenarioBuilder WithFreeParking(uint amount) { _freeParkingAmount = amount; return this; }

    /// <summary>Starts a global event on the game (utility/station rent, tax, real Free Parking, jail-full).</summary>
    public GameScenarioBuilder WithGlobalEvent(GlobalEvent ev, ushort? multiplier = null)
    { _globalEvent = new EventInfo(ev, multiplier); return this; }

    /// <summary>
    /// Pre-sets <see cref="GameModel.ModifiedDiceRollType"/> — the flag a convert/downgrade card sets.
    /// Normally a card sets this mid-roll; exposed only to isolate a state a card would produce.
    /// </summary>
    public GameScenarioBuilder WithModifiedRollType(DiceRollType? rollType) { _modifiedRollType = rollType; return this; }

    /// <summary>Adds a player, configured via <paramref name="configure"/>. Order added = seat order unless overridden.</summary>
    public GameScenarioBuilder WithPlayer(string id, Action<PlayerSpec>? configure = null)
    {
        var spec = new PlayerSpec(id) { OrderId = (ushort)_players.Count };
        configure?.Invoke(spec);
        _players.Add(spec);
        return this;
    }

    /// <summary>
    /// Pins the card of deck <paramref name="type"/> with text <paramref name="text"/> to the <b>front</b>
    /// of its deck, so the next in-turn draw of that type yields it (e.g. force which Triple card a triple
    /// roll draws). This arranges initial deck <i>order</i> only — the draw itself runs through the real
    /// <c>CardService</c>. Multiple calls stack front-to-back in call order.
    /// </summary>
    public GameScenarioBuilder WithNextCard(CardType type, string text) { _nextCards.Add((type, text)); return this; }

    /// <summary>
    /// Seeds a keep-until-needed card into <paramref name="playerId"/>'s hand at build time by pinning it
    /// to its deck and drawing it through the real <c>CardService.DrawCard</c> (real keep/hand-back logic).
    /// The card must be a held card (non-<see cref="CardConditionType.None"/>) or the draw would resolve it
    /// on the spot — the builder asserts this.
    /// </summary>
    public GameScenarioBuilder WithHeldCard(string playerId, CardType type, string text)
    { _heldCards.Add((playerId, type, text)); return this; }

    /// <summary>
    /// Builds the scenario. <paramref name="prompts"/> defaults to a console-driven provider; pass a
    /// scripted <see cref="IPromptProvider"/> to drive without a terminal.
    /// </summary>
    public async Task<GameScenario> BuildAsync(IPromptProvider? prompts = null)
    {
        if (_players.Count == 0)
            throw new InvalidOperationException("A scenario needs at least one player.");

        var provider = TestHarness.BuildServiceProvider();
        var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        var board = TestHarness.DefaultBoard();

        var cards = await sp.GetRequiredService<ICardCacheService>().GetCards();
        AssertUniqueCardIds(cards);

        var properties = sp.GetRequiredService<PropertyService>().GetProperties(board);
        ApplyOwnership(properties);

        var players = _players.Select(BuildPlayer).ToList();

        var currentPlayerId = _currentPlayerId ?? _players[0].Id;
        if (players.All(p => p.PlayerId != currentPlayerId))
            throw new InvalidOperationException($"Current player '{currentPlayerId}' is not one of the scenario's players.");

        var game = new GameModel
        {
            GameId = _gameId,
            Metadata = new TurnMetadata
            {
                CurrentTurnId = "turn-1",
                CurrentPlayerId = currentPlayerId,
                TurnNumber = _turnNumber
            },
            ReserveRuleActive = _reserveRuleActive,
            FreeParkingAmount = _freeParkingAmount,
            GlobalEventInfo = _globalEvent ?? new EventInfo(),
            ModifiedDiceRollType = _modifiedRollType,
            Players = players,
            Properties = properties,
            CardDecks = CardDeckHelper.BuildCardDecks(cards)
        };

        var dto = new GameDTO(
            id: _gameId,
            name: "Test Game",
            boardId: null,
            roundingRule: _rounding,
            hostPlayerId: _hostPlayerId,
            state: GameState.InPlay,
            outcome: GameOutcome.None);

        var cache = new GameCacheModel(dto, game, board);

        // Seed cards on an auto-acknowledging provider, then swap in the real drive provider. Seeding
        // draws held cards through the genuine CardService so keep/hand-back logic isn't reimplemented;
        // the builder only arranges initial deck order (pinning), which is snapshot state.
        var engine = TestHarness.BuildEngine(sp, cache, new AutoAcknowledgePromptProvider());
        await SeedHeldCards(engine, cards);
        PinNextCards(cache.Game.CardDecks, cards);
        engine.PromptProvider = prompts ?? new ConsolePromptProvider();

        return new GameScenario(provider, scope, cache, engine);
    }

    private async Task SeedHeldCards(MP.GameEngine.Services.Framework.GameEngine engine, List<CardModel> cards)
    {
        foreach (var (playerId, type, text) in _heldCards)
        {
            var card = ResolveCard(cards, type, text);
            if (!card.IsKeepUntilNeeded)
                throw new InvalidOperationException(
                    $"WithHeldCard requires a keep-until-needed card, but \"{text}\" ({type}) is resolve-on-draw.");

            var player = engine.Cache.Game.GetPlayer(playerId)
                         ?? throw new InvalidOperationException($"Held-card player '{playerId}' not found (or bankrupt).");

            PinToTop(engine.Cache.Game.CardDecks, type, card.CardId);
            await engine.CardService.DrawCard(engine, player, type, CancellationToken.None);
        }
    }

    private void PinNextCards(CardListModel decks, List<CardModel> cards)
    {
        // Reverse so the first WithNextCard call ends up frontmost after successive pins.
        foreach (var (type, text) in Enumerable.Reverse(_nextCards))
            PinToTop(decks, type, ResolveCard(cards, type, text).CardId);
    }

    private PlayerModel BuildPlayer(PlayerSpec s)
        => new()
        {
            PlayerId = s.Id,
            OrderId = s.OrderId,
            Dice1 = s.Dice1,
            Dice2 = s.Dice2,
            Money = s.Money,
            BoardIndex = s.BoardIndex,
            Direction = s.Direction,
            HasPassedInitialGo = s.HasPassedInitialGo,
            InitialRoll = false,
            DoublesInRow = s.DoublesInRow,
            TriplesInRow = s.TriplesInRow,
            TripleBonus = s.TripleBonus,
            JailCost = s.JailCost,
            JailTurnCounter = s.JailTurnCounter,
            TurnsToMiss = s.TurnsToMiss,
            ExtraTurns = s.ExtraTurns,
            IsBankrupt = s.IsBankrupt,
            Loans = s.Loans.Select(a => new LoanModel(a)).ToList()
        };

    private void ApplyOwnership(List<PropertyModel> properties)
    {
        foreach (var s in _players)
        {
            foreach (var set in s.OwnedSets)
                foreach (var index in PropertySetHelper.GetIndexes(set))
                    SetOwnership(properties, index, s.Id, PropertyState.Owned, RentLevel.SET);

            foreach (var (index, state, rentLevel) in s.OwnedProperties)
                SetOwnership(properties, index, s.Id, state, rentLevel);
        }
    }

    private static void SetOwnership(List<PropertyModel> properties, ushort index, string ownerId,
        PropertyState state, RentLevel rentLevel)
    {
        var property = properties.FirstOrDefault(p => p.BoardIndex == index)
                       ?? throw new InvalidOperationException($"No property at board index {index}.");
        property.OwnerPlayerId = ownerId;
        property.State = state;
        property.RentLevel = rentLevel;
        property.StreetRuleQualifier = StreetRuleQualifier.NeverBuiltOn;
    }

    private static CardModel ResolveCard(List<CardModel> cards, CardType type, string text)
    {
        var matches = cards.Where(c => c.CardType == type && c.CardText == text).ToList();
        return matches.Count switch
        {
            0 => throw new InvalidOperationException($"No {type} card with text \"{text}\"."),
            > 1 => throw new InvalidOperationException($"Ambiguous {type} card text \"{text}\" ({matches.Count} matches)."),
            _ => matches[0]
        };
    }

    /// <summary>Rebuilds <paramref name="type"/>'s deck queue with <paramref name="cardId"/> moved to the front (initial deck order — snapshot state).</summary>
    private static void PinToTop(CardListModel decks, CardType type, string cardId)
    {
        var rest = DeckQueue(decks, type).Where(id => id != cardId);
        SetDeck(decks, type, new[] { cardId }.Concat(rest));
    }

    private static Queue<string> DeckQueue(CardListModel decks, CardType type)
        => type switch
        {
            CardType.Chance => decks.ChanceCards,
            CardType.ComChest => decks.CommunityChestCards,
            CardType.PercentageChance => decks.PercentChanceCards,
            CardType.PercentageComChest => decks.PercentCommunityChestCards,
            CardType.Third => decks.ThirdCards,
            CardType.Double => decks.DoubleCards,
            CardType.Triple => decks.TripleCards,
            CardType.Tax => decks.TaxCards,
            CardType.Go => decks.GoCards,
            CardType.JustVisiting => decks.JustVisitingCards,
            CardType.FreeParking => decks.FreeParkingCards,
            CardType.GoToJail => decks.GoToJailCards,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };

    private static void SetDeck(CardListModel decks, CardType type, IEnumerable<string> ids)
    {
        var queue = new Queue<string>(ids);
        switch (type)
        {
            case CardType.Chance: decks.ChanceCards = queue; break;
            case CardType.ComChest: decks.CommunityChestCards = queue; break;
            case CardType.PercentageChance: decks.PercentChanceCards = queue; break;
            case CardType.PercentageComChest: decks.PercentCommunityChestCards = queue; break;
            case CardType.Third: decks.ThirdCards = queue; break;
            case CardType.Double: decks.DoubleCards = queue; break;
            case CardType.Triple: decks.TripleCards = queue; break;
            case CardType.Tax: decks.TaxCards = queue; break;
            case CardType.Go: decks.GoCards = queue; break;
            case CardType.JustVisiting: decks.JustVisitingCards = queue; break;
            case CardType.FreeParking: decks.FreeParkingCards = queue; break;
            case CardType.GoToJail: decks.GoToJailCards = queue; break;
            default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    /// <summary>
    /// Guards the invariant the decks depend on: a card id must resolve to exactly one card across
    /// <i>all</i> decks (the deck queues store ids, and <c>GetCard(id)</c> searches the whole list).
    /// Fails loudly with the colliding ids rather than letting a draw silently resolve the wrong card.
    /// </summary>
    private static void AssertUniqueCardIds(List<CardModel> cards)
    {
        var dupes = cards.GroupBy(c => c.CardId).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (dupes.Count > 0)
            throw new InvalidOperationException(
                $"CardCacheMock produced duplicate CardIds across decks ({string.Join(", ", dupes)}). " +
                "Card ids must be globally unique or GetCard(id) resolves the wrong card.");
    }
}

/// <summary>Per-player configuration for <see cref="GameScenarioBuilder.WithPlayer"/>. Mid-game defaults.</summary>
public sealed class PlayerSpec(string id)
{
    public string Id { get; } = id;
    public ushort OrderId { get; set; }
    public ushort Dice1 { get; set; } = 2;
    public ushort Dice2 { get; set; } = 5;
    public uint Money { get; set; } = 1500;
    public ushort BoardIndex { get; set; }
    public PlayerDirection Direction { get; set; } = PlayerDirection.Forward;
    public bool HasPassedInitialGo { get; set; } = true;   // mid-game: GO has been passed
    public ushort DoublesInRow { get; set; }
    public ushort TriplesInRow { get; set; }
    public uint TripleBonus { get; set; } = RuleDictionary.DefaultTripleBonus;
    public uint JailCost { get; set; } = RuleDictionary.DefaultJailCost;
    public ushort JailTurnCounter { get; set; }
    public ushort TurnsToMiss { get; set; }
    public ushort ExtraTurns { get; set; }
    public bool IsBankrupt { get; set; }

    internal List<PropertySet> OwnedSets { get; } = [];
    internal List<(ushort Index, PropertyState State, RentLevel RentLevel)> OwnedProperties { get; } = [];
    internal List<uint> Loans { get; } = [];

    public PlayerSpec Order(ushort orderId) { OrderId = orderId; return this; }
    public PlayerSpec Dice(ushort d1, ushort d2) { Dice1 = d1; Dice2 = d2; return this; }
    public PlayerSpec WithMoney(uint money) { Money = money; return this; }
    public PlayerSpec At(ushort boardIndex) { BoardIndex = boardIndex; return this; }
    public PlayerSpec Facing(PlayerDirection direction) { Direction = direction; return this; }
    public PlayerSpec PassedGo(bool passed = true) { HasPassedInitialGo = passed; return this; }
    public PlayerSpec Doubles(ushort inRow) { DoublesInRow = inRow; return this; }
    public PlayerSpec Triples(ushort inRow) { TriplesInRow = inRow; return this; }
    public PlayerSpec WithTripleBonus(uint bonus) { TripleBonus = bonus; return this; }

    /// <summary>Places the player in jail (board index = jail space) with an optional turn counter.</summary>
    public PlayerSpec InJail(ushort turnCounter = 0)
    { BoardIndex = IndexHelper.JailSpace; JailTurnCounter = turnCounter; return this; }

    /// <summary>Grants a complete colour set at <see cref="RentLevel.SET"/> (no buildings).</summary>
    public PlayerSpec OwnsSet(PropertySet set) { OwnedSets.Add(set); return this; }

    /// <summary>Grants a single property at the given state/rent level.</summary>
    public PlayerSpec Owns(ushort boardIndex, PropertyState state = PropertyState.Owned,
        RentLevel rentLevel = RentLevel.SINGLE)
    { OwnedProperties.Add((boardIndex, state, rentLevel)); return this; }

    /// <summary>Adds an outstanding loan of <paramref name="amount"/>.</summary>
    public PlayerSpec WithLoan(uint amount) { Loans.Add(amount); return this; }
}

/// <summary>
/// A built scenario: the <see cref="Cache"/>, a wired <see cref="Engine"/> (with the console prompt
/// provider set), and the DI scope its sub-services live in. Resolve a service with
/// <see cref="Resolve{T}"/> and drive it with <see cref="Engine"/>. Dispose to tear down the scope.
/// </summary>
public sealed class GameScenario(ServiceProvider provider, IServiceScope scope,
    GameCacheModel cache, MP.GameEngine.Services.Framework.GameEngine engine) : IDisposable
{
    public GameCacheModel Cache { get; } = cache;
    public MP.GameEngine.Services.Framework.GameEngine Engine { get; } = engine;

    /// <summary>Resolves a scoped engine service (orchestrator, dice/card/board services, …) from the scenario's scope.</summary>
    public T Resolve<T>() where T : notnull => scope.ServiceProvider.GetRequiredService<T>();

    /// <summary>The turn orchestrator — the usual entry point for driving a full turn.</summary>
    public PlayerTurnOrchestrator Orchestrator => Resolve<PlayerTurnOrchestrator>();

    public void Dispose()
    {
        scope.Dispose();
        provider.Dispose();
    }
}