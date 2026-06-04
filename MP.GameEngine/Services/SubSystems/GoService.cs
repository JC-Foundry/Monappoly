using MP.GameEngine.Enums.Players;
using MP.GameEngine.Helpers.RuleSet;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.SubSystems;

public class GoService
{
    private readonly TransactionService _transactionService;
    private readonly LoanService _loanService;
    private readonly PropertyService _propertyService;

    public GoService(TransactionService transactionService,
        LoanService loanService,
        PropertyService propertyService)
    {
        _transactionService = transactionService;
        _loanService = loanService;
        _propertyService = propertyService;
    }
    
    public async Task CollectGoMoney(Framework.GameEngine engine, PlayerModel player, ushort goPasses, CancellationToken ct)
    {
        var bonus = player.Direction switch
        {
            PlayerDirection.Forward => RuleDictionary.GoPassClockwiseBonus,
            PlayerDirection.Backward => RuleDictionary.GoPassCounterClockwiseBonus,
            _ => throw new ArgumentOutOfRangeException(nameof(player), player, null)
        };
        
        bonus *= goPasses;
        await _transactionService.ReceiveGoBonus(engine, player, bonus, ct);
        
        //Pay mortage fee (no-ops if no mortgages):
        await _propertyService.PayMortgageFee(engine, player, ct);
        
        //Repay any loans (no-ops if no loans):
        await _loanService.ForcedRepayLoans(engine, player, ct);

        //Player has now passed GO (assumed since they are collecting bonus)
        player.HasPassedInitialGo = true;
    }
}