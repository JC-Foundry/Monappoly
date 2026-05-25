using MP.GameEngine.Abstractions;

namespace UltimateMonopoly.Services.GameEngine;

public class GameEngineFactory : IGameEngineFactory
{
    public Task<MP.GameEngine.Services.Framework.GameEngine> GetAsync(string gameId)
    {
        return null;
    }
}