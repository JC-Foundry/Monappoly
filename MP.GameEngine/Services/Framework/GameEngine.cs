using MP.GameEngine.Abstractions;
using MP.GameEngine.Models;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Services.Framework;

public sealed class GameEngine(GameCacheModel cache, 
    ISnapshotService snapshotService, 
    IEngineNotifier notifier, 
    IShortfallService shortfallService)
{
    public GameCacheModel Cache { get; } = cache;
    public IPromptProvider PromptProvider { get; } = new PromptProvider(cache, notifier);
    public ITurnStateProvider TurnStateProvider { get; } = new TurnStateProvider(cache, snapshotService);
    public IEventEmitter EventEmitter { get; } = new EventEmitter(cache);
    public IEngineNotifier Notifier { get; } = notifier;
    
    public IShortfallService ShortfallService { get; } = shortfallService;
}