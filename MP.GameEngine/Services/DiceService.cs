using MP.GameEngine.Abstractions;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Prompts.PromptTypes.Responses;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services;

public class DiceService
{
    private readonly IGameEngineFactory _engineFactory;
    
    public DiceService(IGameEngineFactory engineFactory)
    {
        _engineFactory = engineFactory;
    }

    private async Task<Framework.GameEngine> GetCache(string gameId)
        => await _engineFactory.GetAsync(gameId);

    public async Task RollTurnDice(string gameId)
    {
        var engine = await GetCache(gameId);

        var dice = await engine.PromptProvider.RequestAsync(new DiceRollPrompt
        {
            PlayerId = engine.Cache.Game.Metadata.CurrentPlayerId,
            Title = "Its Your Turn",
            Body = "Roll the dice to start your turn",
            DiceCount = 3
        });
        
        var isDouble = IsDouble(dice);
        if (isDouble) return;   //TODO call double dice helper; which decides what happens for players

        var isTriple = IsTriple(dice);
        if (isTriple)
    }
    
    
    private bool IsDouble(DiceRollResponse dice)
        => dice.Die1 == dice.Die2 && dice.ThirdDie != dice.Die1 && dice.ThirdDie != dice.Die2;
    
    private bool IsTriple(DiceRollResponse dice)
        => dice.Die1 == dice.Die2 && dice.Die2 == dice.ThirdDie;

    private void MoveOnTriple(GameModel game, DiceRollResponse dice)
    {
        var move = dice.Die1 + dice.Die2;
        if (move == null) throw new InvalidOperationException("Invalid dice roll");

        var currentPlayer = game.CurrentPlayer();
    }
}