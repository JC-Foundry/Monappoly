using Microsoft.Extensions.Caching.Memory;
using MP.GameEngine.Abstractions;
using MP.GameEngine.Models;
using UltimateMonopoly.Services.Games;

namespace UltimateMonopoly.Services.Cache;

public class GameCacheService
{
    private readonly IMemoryCache _cache;
    private readonly IGameEngineFactory _gameEngineFactory;

    private const string CacheKey = "GameCache";
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromHours(12);
    
    public GameCacheService(IMemoryCache cache,
        IGameEngineFactory gameEngineFactory)
    {
        _cache = cache;
        _gameEngineFactory = gameEngineFactory;
    }
    
    private string GetKey(string gameId) => $"{CacheKey}__{gameId}";

    public async Task<GameCacheModel?> GetGame(string gameId)
        => await _cache.GetOrCreateAsync(GetKey(gameId), async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var engine = await _gameEngineFactory.GetAsync(gameId);
            return engine.Cache;
        });
    
    public void PopulateGame(GameCacheModel game)
    {
        if (game == null!) return;
        
        _cache.Set(GetKey(game.GameId), game, CacheExpiration);
    }

    public async Task SaveChangesAsync(string gameId)
    {
        var game = await GetGame(gameId);
        game?.SaveChanges();
    }
}