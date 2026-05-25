using MP.GameEngine.Abstractions;
using MP.GameEngine.Enums;
using MP.GameEngine.Models;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.Framework;

/// <summary>
/// Owns turn-state capability checks and transitions for a single game
/// (one cache, one provider). All command-gating questions and all phase
/// transitions go through here — outside code should never compare
/// <see cref="GameCacheModel.TurnState"/> directly. See
/// <c>design-docs/turn-state.md</c>.
/// </summary>
/// <remarks>
/// The provider is a stateful helper, not the orchestrator. Higher-level
/// turn-loop / caller services decide *when* to call the transitions; the
/// provider only owns the rules of *what is allowed* and *what the next
/// state is*.
/// </remarks>
public class TurnStateProvider(GameCacheModel cache, ISnapshotService snapshotService) : ITurnStateProvider
{
    // ─── Private primitives ──────────────────────────────────────────────

    /// <summary>True when no prompt is awaiting a response — i.e. the engine is not mid-execution.</summary>
    private bool IsEngineIdle() => cache.PendingPrompt is null;

    /// <summary>True when the given player is the one whose turn it currently is.</summary>
    private bool IsCurrentPlayer(string playerId) =>
        cache.Game.Metadata.CurrentPlayerId == playerId;
    
    private PlayerModel CurrentPlayer()
        => cache.Game.Players.FirstOrDefault(p => p.PlayerId == cache.Game.Metadata.CurrentPlayerId) 
           ?? throw new InvalidOperationException("Current player not found in game players list.");

    /// <summary>True when the given player is in jail right now.</summary>
    private bool IsJailed(string playerId)
    {
        var player = cache.Game.Players.FirstOrDefault(p => p.PlayerId == playerId);
        return player?.IsInJail ?? throw new InvalidOperationException("Player not found in game players list.");
    }

    /// <summary>True when the cache is at one of the two idle boundary phases (Start or End of turn).</summary>
    private bool IsAtTurnBoundary() =>
        cache.TurnState is TurnState.StartOfTurn or TurnState.EndOfTurn;


    // ─── Capability gates ───────────────────────────────────────────────
    // Each "Can…" returns true when the named player is allowed to issue
    // the command right now. Composes the primitives above — no boolean
    // spaghetti in callers.

    /// <summary>
    /// Portfolio commands (mortgage / unmortgage / build / sell houses / play
    /// card from hand / pay loan early — anything in the player's portfolio):
    /// StartOfTurn only, current player only, not in jail, engine idle.
    /// EndOfTurn is *not* a portfolio window — once movement is done the
    /// player can only end the turn or initiate/accept a deal.
    /// </summary>
    public bool CanPortfolioCommand(string playerId) =>
        cache.TurnState == TurnState.StartOfTurn
        && IsCurrentPlayer(playerId)
        && !IsJailed(playerId)
        && IsEngineIdle();

    /// <summary>
    /// Deals can be initiated or accepted at either turn boundary (Start or
    /// End). Bilateral validation (the *other* party must also be reachable)
    /// is the engine layer's job — the provider only confirms the calling
    /// player is at a legal moment in their own turn cycle.
    /// </summary>
    public bool CanDeal(string playerId) =>
        IsAtTurnBoundary() && IsEngineIdle();

    /// <summary>
    /// Jail exit (pay fee / play card / attempt double): only at StartOfTurn,
    /// only by the current player, only if they're actually in jail.
    /// </summary>
    public bool CanLeaveJail(string playerId) =>
        cache.TurnState == TurnState.StartOfTurn
        && IsCurrentPlayer(playerId)
        && IsJailed(playerId)
        && IsEngineIdle();

    /// <summary>End turn: current player only, at EndOfTurn, engine idle.</summary>
    public bool CanEndTurn(string playerId) =>
        cache.TurnState == TurnState.EndOfTurn
        && IsCurrentPlayer(playerId)
        && IsEngineIdle();

    /// <summary>
    /// Voluntary bankruptcy: any player, at either turn boundary, engine idle.
    /// "At any time" in <c>game-rules.md</c> Bankruptcy rule 1 means at any
    /// of their own (or another player's) turn boundaries — not literally any
    /// moment in the middle of execution.
    /// </summary>
    public bool CanDeclareBankruptcy(string playerId) =>
        IsAtTurnBoundary() && IsEngineIdle();


    // ─── Transitions ────────────────────────────────────────────────────
    // Named transitions encode the branches of the turn loop. Both
    // extra-turn and next-player fire from EndOfTurn, commit, and write a
    // new GameTurn + GameSnapshot pair — they differ only in whether
    // CurrentPlayerId advances (next-player) or stays (extra-turn). Each
    // transition validates the current state before mutating, throwing
    // if called from the wrong place.

    /// <summary>StartOfTurn → PlayerRollMovement. The player has finished any portfolio commands and is rolling.</summary>
    public void TransitionToRollPhase()
    {
        Expect(TurnState.StartOfTurn);
        cache.SetTurnState(TurnState.PlayerRollMovement);
    }

    /// <summary>
    /// PlayerRollMovement → ThirdDieMovement. Normal roll or non-triple
    /// double — the roller has moved and now the other players take the
    /// third die.
    /// </summary>
    public void TransitionToThirdDie()
    {
        Expect(TurnState.PlayerRollMovement);
        cache.SetTurnState(TurnState.ThirdDieMovement);
    }
    
    /// <summary>
    /// → EndOfTurn. Allowed from PlayerRollMovement (triple with no extra
    /// roll due, or 3-in-a-row sending the roller to jail) or
    /// ThirdDieMovement (normal end of turn — other players have moved and
    /// no extra roll was triggered).
    /// </summary>
    public void TransitionToEndOfTurn()
    {
        if (cache.TurnState is not (TurnState.PlayerRollMovement or TurnState.ThirdDieMovement))
            throw new InvalidOperationException(
                $"TransitionToEndOfTurn requires PlayerRollMovement or ThirdDieMovement, got {cache.TurnState}.");

        cache.SetTurnState(TurnState.EndOfTurn);
    }

    /// <summary>
    /// EndOfTurn → StartOfTurn for the *same* player — an extra turn
    /// granted by a double, triple, or card. Fires from EndOfTurn (not
    /// directly from the movement phases), so the player has had the
    /// EndOfTurn idle window to settle deals before rolling their extra
    /// turn. Commits the working state, clears the per-turn event window,
    /// bumps the matching <c>DoublesInRow</c> / <c>TriplesInRow</c> counter
    /// (and resets the other per <c>game-rules.md</c> Doubles/Triples rule
    /// 6), advances turn metadata (new <c>CurrentTurnId</c>,
    /// <c>TurnNumber</c>++; <c>CurrentPlayerId</c> **unchanged**), and
    /// writes a snapshot via <see cref="ISnapshotService.CreateSnapshotAsync"/>
    /// — a new <c>GameTurn</c> row plus its <c>GameSnapshot</c>. At the
    /// schema level the only thing distinguishing an extra-turn record from
    /// a next-player record is that consecutive <c>GameTurn</c> rows share
    /// <c>CurrentPlayerId</c>; see <see cref="TransitionToNextPlayer"/> for
    /// the other path. The engine does not know *how* the snapshot is
    /// persisted — only that one is taken at this boundary (see
    /// <c>game-engine.md</c> §3).
    /// </summary>
    public async Task TransitionToExtraTurn(bool isTriple)
    {
        Expect(TurnState.EndOfTurn);

        var player = CurrentPlayer();
        if (isTriple)
        {
            player.TriplesInRow++;
            player.DoublesInRow = 0;
        }
        else
        {
            player.DoublesInRow++;
            player.TriplesInRow = 0;
        }
        
        UpdateMetadata(player.PlayerId);

        cache.SaveChanges();
        cache.ClearEvents();
        cache.SetTurnState(TurnState.StartOfTurn);
        
        await snapshotService.CreateSnapshotAsync(cache.Game);
        cache.SaveChanges();
    }

    /// <summary>
    /// EndOfTurn → StartOfTurn (for the next player). Commits the working
    /// game state, clears the per-turn event list, and writes a snapshot
    /// via <see cref="ISnapshotService.CreateSnapshotAsync"/> — a new
    /// <c>GameTurn</c> row plus its <c>GameSnapshot</c> for the turn just
    /// beginning. The engine does not know *how* the snapshot is persisted
    /// — only that one is taken at this boundary (see
    /// <c>game-engine.md</c> §3). Broadcasting is the caller's job;
    /// post-call state can be read from <c>cache.Game</c>.
    /// </summary>
    public async Task TransitionToNextPlayer()
    {
        Expect(TurnState.EndOfTurn);

        AdvancePlayer();

        cache.SaveChanges();
        cache.ClearEvents();
        cache.SetTurnState(TurnState.StartOfTurn);
        
        await snapshotService.CreateSnapshotAsync(cache.Game);
        cache.SaveChanges();
    }


    // ─── Internals ──────────────────────────────────────────────────────

    private void Expect(TurnState expected)
    {
        if (cache.TurnState != expected)
            throw new InvalidOperationException(
                $"Transition requires TurnState={expected}, got {cache.TurnState}.");
    }

    /// <summary>
    /// Advances <see cref="TurnMetadata.CurrentPlayerId"/> to the next
    /// player in seat order (wraps around). Intrinsic to "next player"
    /// but borders on game logic.
    /// </summary>
    /// <remarks>
    /// TODO: extract into a dedicated helper class once the turn-loop
    /// orchestration shape is clearer. The helper should sit *above* the
    /// provider and decide which transition fires (extra-turn vs next-
    /// player), and own the harder cases this stub doesn't handle yet:
    /// skip bankrupt players (<c>game-rules.md</c> Bankruptcy) and
    /// decrement <see cref="PlayerModel.TurnsToMiss"/> to skip missed-
    /// turn players (Double 2 effect). Keeping it here would leak more
    /// game logic into the foundation provider; pulling it out keeps
    /// the provider a pure state-machine.
    /// </remarks>
    private void AdvancePlayer()
    {
        var currentPlayer = CurrentPlayer();
        var allPlayers = cache.Game.Players.OrderBy(p => p.OrderId).ToList();

        var nextPlayer = allPlayers.FirstOrDefault(p => p.OrderId > currentPlayer.OrderId)
                         ?? allPlayers.MinBy(p => p.OrderId)
                         ?? throw new InvalidOperationException("No eligible players left in game.");
        UpdateMetadata(nextPlayer.PlayerId);
    }

    private void UpdateMetadata(string playerId)
    {
        // CurrentTurnId is not assigned here — the snapshot service
        // generates the new GameTurn id and writes it back to
        // Metadata.CurrentTurnId as part of CreateSnapshotAsync.
        cache.Game.Metadata.CurrentPlayerId = playerId;
        cache.Game.Metadata.TurnNumber++;
    }
}