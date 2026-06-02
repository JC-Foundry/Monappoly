using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Properties;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.SubSystems;

public class BoardService
{
    private readonly GoService _goService;
    private readonly JailService _jailService;
    private readonly FreeParkingService _fpService;
    private readonly PropertyService _propertyService;

    public BoardService(GoService goService, 
        JailService jailService,
        FreeParkingService fpService,
        PropertyService propertyService)
    {
        _goService = goService;
        _jailService = jailService;
        _fpService = fpService;
        _propertyService = propertyService;
    }
    
    public async Task ResolveBoardSpaceForPlayer(Framework.GameEngine engine, PlayerModel player, CancellationToken ct)
    {
        var space = engine.Cache.Board.GetBoardSpace(player.BoardIndex);
        
        var propertySpace = engine.Cache.Game.GetPropertySpace(space.Index);
        if (propertySpace is not null)
        {
            switch (propertySpace.State)
            {
                case PropertyState.NotOwned:
                    if(player.HasPassedInitialGo)
                        await _propertyService.ProcessUnownedProperty(engine, player, space, propertySpace, ct);
                    break;
                case PropertyState.FreeParking:
                    //Only needs the board space info (rents), "single" rent always assumed for FP property
                    //propertySpace is not needed as state already validated as being in FP
                    await _fpService.PayPropertyFee(engine, player, space, ct);
                    break;
                case PropertyState.Owned:
                    await _propertyService.PayPropertyRent(engine, player, space, propertySpace, ct);
                    break;
                case PropertyState.Mortgaged:
                case PropertyState.Reserved:
                default:
                    //nothing happens
                    break;
            }
        }
        else
        {
            switch (space.SpaceType)
            {
                case BoardSpaceType.Tax:
                    //TODO Call tax service to get tax card, then pay tax/do what card says
                    break;
                case BoardSpaceType.Chance:
                    //TODO Call Card service to get card, and do what card says
                    break;
                case BoardSpaceType.ComChest:
                    //TODO call card service to get card, and do what card says
                    break;
                case BoardSpaceType.Go:
                    //TODO call GO service to get GO card, and collect 200/do what card says
                    break;
                case BoardSpaceType.JustVisiting:
                    //TODO - Call just visiting service to get just visiting card, and do what card says
                    break;
                case BoardSpaceType.FreeParking:
                    //TODO - Call free parking service to get free parking card, and do what card says and/or proceed as normal
                    break;
                case BoardSpaceType.GoToJail:
                    //TODO - call jail service to get jail card, and do what card says and/or go to jail
                    break;
                case BoardSpaceType.Property:
                case BoardSpaceType.Station:
                case BoardSpaceType.Utility:
                    //Property should be in list of properties
                    throw new InvalidOperationException("Property should be in list of properties");
                case BoardSpaceType.Jail:
                default:
                    //nothing, since jail is no-op
                    break;
            }
        }
    }
}