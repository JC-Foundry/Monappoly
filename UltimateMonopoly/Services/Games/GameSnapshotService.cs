using JC.Core.Models;
using JC.Core.Services.DataRepositories;
using MP.GameEngine.Models;

namespace UltimateMonopoly.Services.Games;

public class GameSnapshotService
{
    private readonly IRepositoryManager _repos;
    private readonly IUserInfo _userInfo;

    public GameSnapshotService(IRepositoryManager repos,
        IUserInfo userInfo)
    {
        _repos = repos;
        _userInfo = userInfo;
    }

    public async Task<GameCacheModel?> GetGameCacheModel(string gameId)
    {
        //TODO - Implement method that:
        // - Gets the game snapshot, deserialises the json
        // - Creates the cache model (at start of turn), and populates the board object
        return null;
    }
}