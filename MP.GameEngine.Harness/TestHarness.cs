using Microsoft.Extensions.DependencyInjection;
using MP.GameEngine.Abstractions;
using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Extensions;
using MP.GameEngine.Models;
using MP.GameEngine.Models.Boards;
using MP.GameEngine.Models.EventReceipts;
using MP.GameEngine.Models.Imports;
using MP.GameEngine.Models.Prompts;
using MP.GameEngine.Models.Snapshot;
using MP.GameEngine.Services.Cards;
using System.Text.Json;

namespace MP.GameEngine.Harness;

/// <summary>
/// Harness wiring that mirrors how the web layer builds a live engine, minus the web/EF seams.
/// It stands up a DI container from <see cref="ServiceRegistration.AddGameEngine"/> (the real engine
/// service graph), swaps in the four contracts the engine leaves to the web layer for no-op doubles
/// (<see cref="ISnapshotService"/>, <see cref="IEngineNotifier"/>, <see cref="IGameCompletionService"/>,
/// <see cref="ITurnTaxService"/>) and the <see cref="CardCacheMock"/> for <see cref="ICardCacheService"/>,
/// and builds a <see cref="MP.GameEngine.Services.Framework.GameEngine"/> bundle around a supplied
/// <see cref="GameCacheModel"/> — exactly as <c>GameEngineFactory</c> does.
/// </summary>
public static class TestHarness
{
    /// <summary>Board config path, relative to the harness project directory (see <c>Program</c>'s cwd setup).</summary>
    private const string BoardPath = @"..\config\boards\board.json";

    private static Board? _cachedBoard;

    /// <summary>
    /// Loads the real default board from <c>config/boards/board.json</c>, mirroring
    /// <c>BoardImportService.ImportDefaultBoard</c> (construct each space, then apply its rents).
    /// Cached after the first load — the board is immutable for a game's lifetime.
    /// </summary>
    public static Board DefaultBoard()
    {
        if (_cachedBoard is not null) return _cachedBoard;

        var text = File.ReadAllText(BoardPath);
        var imports = JsonSerializer.Deserialize<List<BoardSpaceJsonImport>>(text)
                      ?? throw new InvalidOperationException("Failed to deserialise board data from file");

        var spaces = new List<BoardSpace>();
        foreach (var import in imports)
        {
            var space = new BoardSpace(import);
            if (import.Rents != null && !space.SetRents(import.Rents))
                throw new InvalidOperationException($"Failed to set rents for board space {import.Index}");
            spaces.Add(space);
        }

        _cachedBoard = new Board("Test Board", spaces);
        return _cachedBoard;
    }

    /// <summary>Builds a fresh DI provider with the real engine graph and the harness doubles wired in.</summary>
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddGameEngine();

        // The card cache and the four web-implemented seams the engine graph depends on.
        services.AddSingleton<ICardCacheService, CardCacheMock>();
        services.AddSingleton<ISnapshotService, NoOpSnapshotService>();
        services.AddSingleton<IEngineNotifier, NoOpEngineNotifier>();
        services.AddSingleton<IGameCompletionService, NoOpGameCompletionService>();
        services.AddSingleton<ITurnTaxService, NoOpTurnTaxService>();

        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Builds a <see cref="MP.GameEngine.Services.Framework.GameEngine"/> bundle around
    /// <paramref name="cache"/> exactly as <c>GameEngineFactory</c> does (foundation providers resolved
    /// from <paramref name="scoped"/>), then swaps its prompt provider for a console-driven one.
    /// <c>GameEngine.PromptProvider</c> is <c>internal set</c> for this purpose — pass a scripted
    /// <paramref name="prompts"/> to drive a scenario deterministically instead of via the console.
    /// </summary>
    public static MP.GameEngine.Services.Framework.GameEngine BuildEngine(
        IServiceProvider scoped, GameCacheModel cache, IPromptProvider? prompts = null)
    {
        var engine = new MP.GameEngine.Services.Framework.GameEngine(
            cache,
            scoped.GetRequiredService<ISnapshotService>(),
            scoped.GetRequiredService<IEngineNotifier>(),
            scoped.GetRequiredService<IShortfallService>(),
            scoped.GetRequiredService<CardService>());

        engine.PromptProvider = prompts ?? new ConsolePromptProvider();
        return engine;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Doubles for the web-implemented engine seams
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>No-op snapshot persistence — the transitions call it but the harness doesn't persist.</summary>
public sealed class NoOpSnapshotService : ISnapshotService
{
    public Task CreateSnapshotAsync(GameModel game, bool completeTransaction = true, bool finalTurn = false)
        => Task.CompletedTask;

    public Task CreateTurnEventSnapshotAsync(string gameId, string turnId, List<EventReceipt> receipts)
        => Task.CompletedTask;
}

/// <summary>No-op notifier — the harness reads the cache directly rather than broadcasting.</summary>
public sealed class NoOpEngineNotifier : IEngineNotifier
{
    public void PromptOpened(string gameId, Prompt prompt, string concurrencyStamp) { }
    public void PromptClosed(string gameId, string promptId, string concurrencyStamp) { }
    public void StateChanged(GameCacheModel cache) { }
    public void Notify(string gameId, string targetPlayerId, string message) { }
    public void GameCompleted(string gameId) { }
    public void ForceRefresh(string gameId) { }
    public void GameCancelled(string gameId) { }
}

/// <summary>
/// No-op completion service. Never fires in a scenario with two or more active players; present so the
/// orchestrator's single-player winner short-circuit resolves without a real completion pipeline.
/// </summary>
public sealed class NoOpGameCompletionService : IGameCompletionService
{
    public Task DeclareWinner(MP.GameEngine.Services.Framework.GameEngine engine) => Task.CompletedTask;
    public Task DrawGame(MP.GameEngine.Services.Framework.GameEngine engine) => Task.CompletedTask;
    public Task<bool> TryDrawGameByAdmin(MP.GameEngine.Services.Framework.GameEngine engine, bool isAdmin = false)
        => Task.FromResult(true);
}

/// <summary>Turn tax disabled — <see cref="ApplyTax"/> is a no-op so it never perturbs a scenario's cash.</summary>
public sealed class NoOpTurnTaxService : ITurnTaxService
{
    public bool Enabled => false;
    public Task Import() => Task.CompletedTask;
    public Task ApplyTax(MP.GameEngine.Services.Framework.GameEngine engine, PlayerModel player, CancellationToken ct)
        => Task.CompletedTask;
    public uint TotalTax(uint balance) => 0;
}