using MP.GameEngine.Abstractions;
using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Properties;
using MP.GameEngine.Helpers;
using MP.GameEngine.Helpers.RuleSet;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Prompts.PromptTypes.Responses;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.SubSystems;

public class ShortfallService : IShortfallService
{
    private readonly PropertyService _propertyService;
    private readonly LoanService _loanService;

    public ShortfallService(PropertyService propertyService,
        LoanService loanService)
    {
        _propertyService = propertyService;
        _loanService = loanService;
    }
    
    private async Task<List<ushort>> TargetPropertyPrompt(Framework.GameEngine engine, string playerId, string title, string body, 
        List<PropertyModel> properties, CancellationToken ct)
    {
        var response = await engine.PromptProvider.RequestAsync(new TargetPropertyPrompt
        {
            PlayerId = playerId,
            Title = title,
            Body = body,
            EligibleBoardIndexes = properties.Select(p => p.BoardIndex).ToList(),
            Count = 1
        }, ct: ct);

        return response.SelectedBoardIndexes.ToList();
    }

    /// <summary>
    /// Opens a <see cref="ShortfallPrompt"/> and dispatches the chosen action to the
    /// relevant sub-service.
    /// </summary>
    /// <remarks>
    /// Sub-services (LoanService, MortgageService, SellService, DealService,
    /// BankruptcyService) are not yet implemented — see the payment-service-pending
    /// project memory. The branches are TODO-stubbed and currently report the
    /// expected outcome shape so the outer <see cref="Move"/> logic can be wired
    /// against the final contract.
    /// </remarks>
    public async Task<ShortfallOutcome> ResolveShortfall(
        Framework.GameEngine engine,
        PlayerModel player,
        uint shortfallAmount,
        string? owedToPlayerId,
        ushort? counterpartyPropertyIndex,
        CancellationToken ct)
    {
        var remainingShortfall = shortfallAmount;
        while (remainingShortfall > 0)
        {
            var response = await engine.PromptProvider.RequestAsync(new ShortfallPrompt
            {
                PlayerId = player.PlayerId,
                Title = "You can't afford this",
                Body = $"You owe {RuleDictionary.Currency}{shortfallAmount} but only have {RuleDictionary.Currency}{player.Money}. Choose how to settle.",
                PlayerBalance = player.Money,
                Cost = shortfallAmount,
                OwedToPlayerId = owedToPlayerId
            }, ct);

            //TODO: These methods needs to modify remaining shortfall amount:
            //This is so we can keep prompting for shortfall until the player has raised enough.
            //original shortfallAmount variable kept for prompt, and final settlement - shortfallAmount = amount owed
            switch (response.Action)
            {
                case ShortfallAction.TakeLoan:
                    var outcome = await ResolveViaLoan(engine, player, remainingShortfall, ct);
                    if(outcome is not null) 
                        //Loans ALWAYS raise enough funds to pay the shortfall (if can take one out), so we can just return here.
                        return outcome.Value;
                    
                    break;

                case ShortfallAction.Mortgage:
                    await ResolveViaMortgage(engine, player, ct);
                    remainingShortfall = player.Money >= shortfallAmount ? 0 : shortfallAmount - player.Money;
                    break;

                case ShortfallAction.SellHouses:
                    await ResolveViaSell(engine, player, ct);
                    remainingShortfall = player.Money >= shortfallAmount ? 0 : shortfallAmount - player.Money; 
                    break;

                case ShortfallAction.ProposeDeal:
                    //TODO DealService.ProposeSettlingDeal(engine, player, owedToPlayerId!, shortfallAmount, ct);
                    //  A creditor-deal that's accepted DISCHARGES the original debt — the
                    //  deal itself is the settlement (game-rules.md Default rule 7). Return
                    //  DebtSettled so the outer transaction does NOT apply.
                    //  A creditor-deal that's rejected re-opens the shortfall prompt; the
                    //  deal sub-service handles that loop internally before returning here.
                    return ShortfallOutcome.DebtSettled;

                case ShortfallAction.DeclareBankruptcy:
                    //TODO BankruptcyService.Declare(engine, player, owedToPlayerId, ct);
                    return ShortfallOutcome.Bankrupted;

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(response.Action), response.Action, "Unhandled shortfall action.");
            }
        }

        return ShortfallOutcome.FundsRaised;
    }


    private async Task<ShortfallOutcome?> ResolveViaLoan(Framework.GameEngine engine, PlayerModel player, uint shortfall, CancellationToken ct)
    {
        var result = await _loanService.TakeLoanForShortfall(engine, player, shortfall, ct);
        return result ? ShortfallOutcome.FundsRaised : null;
    }
    
    
    
    private async Task ResolveViaMortgage(Framework.GameEngine engine, PlayerModel player, CancellationToken ct)
    {
        var props = engine.Cache.Game.GetOwnedProperties(player.PlayerId, 
            includeMortgaged: false, includeReserved: false);
        props = props.Where(p => p is
        {
            State: PropertyState.Owned,
            RentLevel: RentLevel.SET or RentLevel.SINGLE
        }).ToList();

        var sets = PropertySetHelper.GetOwnedSets(player.PlayerId, props);
        props = props.Where(p => !sets.Contains(PropertySetHelper.ResolveSet(p.BoardIndex) 
                                                //Shouldnt be null; but resolve to utility incase (not buildable anyway, can always be mortgaged)
                                                ?? PropertySet.Utility)).ToList();

        if (props.Count == 0)
        {
            _ = await engine.PromptProvider.Acknowledge(player.PlayerId, "No Properties to Mortgage",
                "You do not have an valid properties to mortgage.", ct: ct);
            return;
        }
        
        var selected = await TargetPropertyPrompt(engine, player.PlayerId,
            "Mortgage a Property to Pay the Shortfall", 
            "You must select a property below to mortgage",
            props, ct);

        foreach (var i in selected)
        {
            await _propertyService.MortgageProperty(engine, i, ct, player.PlayerId);
        }
    }

    
    private async Task ResolveViaSell(Framework.GameEngine engine, PlayerModel player, CancellationToken ct)
    {
        var props = engine.Cache.Game.GetOwnedProperties(player.PlayerId, 
            includeMortgaged: false, includeReserved: false);
        props = props.Where(p => p is
        {
            State: PropertyState.Owned, 
            RentLevel: > RentLevel.SET and <= RentLevel.DOUBLE_HOTEL
        }).ToList();

        var validProps = (from p in props 
            let canSell = engine.Cache.Game.CanDecreaseRentLevel(player.PlayerId, p.BoardIndex) 
            where canSell 
            select p).ToList();
        
        if (validProps.Count == 0)
        {
            _ = await engine.PromptProvider.Acknowledge(player.PlayerId, "No Houses/Hotels to Sell",
                "You do not have an valid properties to sell houses/hotels.", ct: ct);
            return;
        }
        
        var selected = await TargetPropertyPrompt(engine, player.PlayerId,
            "Sell on a Property to Pay the Shortfall", 
            "You must select a property below to sell a house/hotel",
            validProps, ct);

        foreach (var i in selected)
        {
            await _propertyService.SellOnProperty(engine, i, ct, player.PlayerId);
        }
    }
}