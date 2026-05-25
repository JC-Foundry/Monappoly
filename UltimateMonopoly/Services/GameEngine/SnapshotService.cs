using System.Text.Json;
using JC.Core.Services.DataRepositories;
using MP.GameEngine.Abstractions;
using MP.GameEngine.Models.Snapshot;
using UltimateMonopoly.Models.DataModels.Games;

namespace UltimateMonopoly.Services.GameEngine;

public class SnapshotService : ISnapshotService
{
    private readonly IRepositoryManager _repos;
    private readonly ILogger<SnapshotService> _logger;

    public SnapshotService(IRepositoryManager repos,
        ILogger<SnapshotService> logger)
    {
        _repos = repos;
        _logger = logger;
    }

    public async Task CreateSnapshotAsync(GameModel game)
    {
        var turn = new GameTurn(game.GameId, game.Metadata.CurrentPlayerId)
        {
            TurnNumber = game.Metadata.TurnNumber
        };

        game.Metadata.CurrentTurnId = turn.Id;
        var snapshot = new GameSnapshot(turn.Id, game.GameId);

        var stateJson = JsonSerializer.Serialize(game);
        snapshot.StateJson = stateJson;
        
        await _repos.BeginTransactionAsync();
        try
        {
            await _repos.GetRepository<GameTurn>()
                .AddAsync(turn, saveNow: false);
            
            await _repos.GetRepository<GameSnapshot>()
                .AddAsync(snapshot, saveNow: false);
            
            await _repos.SaveChangesAsync();
            await _repos.CommitTransactionAsync();
        }
        catch (Exception ex)
        {
            await _repos.RollbackTransactionAsync();
            _logger.LogError(ex, "Error persisting snapshot for game {GameId}", snapshot.GameId);
            throw;
        }
    }
}