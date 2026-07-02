using System.Data;
using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Players;
using MP.GameEngine.Helpers;
using MP.GameEngine.Helpers.RuleSet;
using MP.GameEngine.Models;
using MP.GameEngine.Models.DTOs;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.SubSystems;

public class PlayerService
{
    private readonly TransactionService _transactionService;

    public PlayerService(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }
    
    public List<PlayerModel> GetPlayers(List<PlayerDTO> playerDtos)
        => playerDtos.Select(playerDto => new PlayerModel
        {
            PlayerId = playerDto.Id,
            OrderId = playerDto.OrderId,
            Dice1 = playerDto.Dice1,
            Dice2 = playerDto.Dice2,
            Money = RuleDictionary.StartingMoney,
            TripleBonus = RuleDictionary.DefaultTripleBonus,
            JailCost = RuleDictionary.DefaultJailCost,
            InitialRoll = true,
            
            //Explicit defaults:
            HasPassedInitialGo = false,
            BoardIndex = 0,
            Direction = PlayerDirection.Clockwise,
            DoublesInRow = 0,
            TriplesInRow = 0,
            TurnsToMiss = 0,
            JailTurnCounter = 0,
            MaxJailTurnsOverride = null,
            IsBankrupt = false
        }).ToList();

    
    
    public async Task ResolveDiceNumber(Framework.GameEngine engine, string playerId, CancellationToken ct)
    {
        var player = engine.Cache.Game.GetPlayer(playerId);
        if (player == null) throw new InvalidOperationException($"Player with id {playerId} not found in game players list.");
        
        var theyRolled = engine.Cache.Game.Metadata.CurrentPlayerId == playerId;
        _ = await engine.PromptProvider.Acknowledge(playerId, "YOUR NUMBER!",
            $"{(theyRolled ? "You rolled" : "Someone else rolled")} your number ({player.Dice1} and {player.Dice2})." +
            $"You will collect {RuleDictionary.Currency}{RuleDictionary.DiceNumRolledBonus} from the bank, " +
            $"{(theyRolled ? $"{RuleDictionary.Currency}{RuleDictionary.DiceNumRolledBonus} from each player, " : "")}" +
            $"and a third card at the end of this turn.",
            ct: ct);
        
        //Set receive third card:
        player.GetThirdCard = true;
        
        //Bank transaction:
        await _transactionService.ReceiveDiceBonus(engine, player, ct);
        if(!theyRolled)
        {
            //Cite dice number rule (for others rolling)
            engine.CiteRule(RuleCode.Roll_DiceNumberByOther);
            return;
        }
        
        var otherPlayers = engine.Cache.Game.GetPlayers(playerId);
        foreach (var p in otherPlayers)
        {
            await _transactionService.PayDiceBonus(engine, p, player, ct);
        }
        
        //Cite dice number rule (for player rolling)
        engine.CiteRule(RuleCode.Roll_DiceNumberBySelf);
    }


    /// <summary>
    /// The default triple-bonus resolution — the holder receives their full bonus (factor ×1) and the
    /// accumulator increments. The orchestrator's triple branch calls this when no Dice card has taken
    /// over the bonus.
    /// </summary>
    public async Task ResolveTripleBonus(Framework.GameEngine engine, PlayerModel holder, CancellationToken ct)
    {
        // Apply the triple bonus exactly once, composing whatever convert/modify/cancel cards recorded onto
        // the pending modifier during the draw + OnTripleBonus window. A recorded cancel (factor 0) wins over
        // any scaling factor; with nothing recorded it is the full ×1 bonus to the holder.
        var modifier = engine.Cache.Game.TripleBonusModifier;
        engine.Cache.Game.TripleBonusModifier = null;

        var factor = modifier is { Cancelled: true } ? (ushort)0 : modifier?.Factor ?? 1;
        var recipient = modifier?.RecipientId is { } id ? engine.Cache.Game.GetPlayer(id) : null;

        await ApplyTripleBonus(engine, holder, factor, recipient, ct);
    }

    /// <summary>
    /// Resolves a triple bonus with a modifiable payout (a Dice <c>ModifyTripleBonus</c> card supplies
    /// a non-default <paramref name="factor"/> / <paramref name="recipient"/>; the default path passes
    /// ×1 to the holder). The payout (<c>base × factor</c>) is credited to <paramref name="recipient"/>
    /// (or the holder when null), and the holder's accumulator <b>always</b> increments by £500 —
    /// even when the payout is suppressed (factor 0) or redirected. See cards-dev-changes.md §2.13.
    /// </summary>
    /// <param name="engine">The game engine bundle the bonus mutates.</param>
    /// <param name="holder">The player who rolled the triple (always accrues the +£500 accumulator).</param>
    /// <param name="factor">Payout multiplier: 0 suppresses it, 1 is the full bonus, 2 doubles it, a die value for ×die.</param>
    /// <param name="recipient">Who receives the payout. Null = the <paramref name="holder"/>; otherwise the bonus is redirected (the holder still accrues the accumulator).</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ApplyTripleBonus(Framework.GameEngine engine, PlayerModel holder, ushort factor,
        PlayerModel? recipient, CancellationToken ct)
    {
        var rule = engine.Cache.RoundingRule;

        var basePayout = MoneyHelper.NormaliseAmountToPositive(holder.TripleBonus, rule, FinancialReason.TripleBonus);
        var payout = MoneyHelper.NormaliseAmountToPositive((long)basePayout * factor, rule, FinancialReason.TripleBonus);

        // Accumulator: always +£500 to the holder, whatever the payout modifier did.
        var newBonus = MoneyHelper.NormaliseAmountToPositive(basePayout + RuleDictionary.TripleBonusIncrease, rule, FinancialReason.TripleBonus);

        //Cite triple bonus rule
        engine.CiteRule(RuleCode.Triple_Bonus);

        recipient ??= holder;
        var redirected = recipient.PlayerId != holder.PlayerId;

        var body = payout == 0
            ? $"You do not receive a triple bonus this time. Your next bonus will be {RuleDictionary.Currency}{newBonus}."
            : redirected
                ? $"Your triple bonus of {RuleDictionary.Currency}{payout} goes to another player. Your next bonus will be {RuleDictionary.Currency}{newBonus}."
                : $"You will receive {RuleDictionary.Currency}{payout} for rolling a triple! Your next bonus will be {RuleDictionary.Currency}{newBonus}.";
        _ = await engine.PromptProvider.Acknowledge(holder.PlayerId, "TRIPLE BONUS!", body, ct: ct);

        if (payout > 0)
            await _transactionService.ReceiveTripleBonus(engine, recipient, payout, ct);

        holder.TripleBonus = newBonus;
    }
}