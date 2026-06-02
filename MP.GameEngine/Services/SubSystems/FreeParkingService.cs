using MP.GameEngine.Models.Boards;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.SubSystems;

public class FreeParkingService
{
    private readonly TransactionService _transactionService;

    public FreeParkingService(TransactionService transactionService)
    {
        _transactionService = transactionService;
    }

    public async Task PayPropertyFee(Framework.GameEngine engine, PlayerModel player, BoardSpace propSpace, CancellationToken ct)
    {
        //TODO: charge the LANDING player the rent value for property and put into FP (free parking)
    }
}