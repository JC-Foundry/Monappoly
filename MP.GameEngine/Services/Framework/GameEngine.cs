using MP.GameEngine.Abstractions;
using MP.GameEngine.Enums;
using MP.GameEngine.Models;
using MP.GameEngine.Services.Cards;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Services.Framework;

public sealed class GameEngine(GameCacheModel cache, 
    ISnapshotService snapshotService, 
    IEngineNotifier notifier, 
    IShortfallService shortfallService,
    CardService cardService)
{
    public GameCacheModel Cache { get; } = cache;
    
    //Internal Set to allow MP.GameEngine.Tests to swap out implementations
    public IPromptProvider PromptProvider { get; internal set; } = new PromptProvider(cache, notifier);
    public ITurnStateProvider TurnStateProvider { get; internal set; } = new TurnStateProvider(cache, snapshotService);
    public IEventEmitter EventEmitter { get; internal set; } = new EventEmitter(cache);
    public IEngineNotifier Notifier { get; } = notifier;
    
    public IShortfallService ShortfallService { get; } = shortfallService;
    public CardService CardService { get; } = cardService;

    /// <summary>
    /// Adds the provided rule code to the game engine's cache.
    /// </summary>
    /// <param name="code">The rule code to be added to the game cache.</param>
    public void CiteRule(RuleCode code)
    {
        //No-op if the code has already been cited.
        if(Cache.RuleCodes.Contains(code))
            return;
            
        Cache.AddRuleCode(code);
    }
}