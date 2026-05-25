namespace MP.GameEngine.Abstractions;

//Implementation created in WebProject
public interface IGameEngineFactory
{
    Task<Services.Framework.GameEngine> GetAsync(string gameId);
}