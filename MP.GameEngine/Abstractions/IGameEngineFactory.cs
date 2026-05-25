namespace MP.GameEngine.Abstractions;

public interface IGameEngineFactory
{
    Task<Services.Framework.GameEngine> GetAsync(string gameId);
}