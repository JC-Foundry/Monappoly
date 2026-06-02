using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Properties;
using MP.GameEngine.Helpers;
using MP.GameEngine.Helpers.RuleSet;
using MP.GameEngine.Models.Boards;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Services.SubSystems;

public class PropertyService
{
    private readonly AuctionService _auctionService;
    private readonly TransactionService _transactionService;

    public PropertyService(AuctionService auctionService,
        TransactionService transactionService)
    {
        _auctionService = auctionService;
        _transactionService = transactionService;
    }

    public List<PropertyModel> GetProperties(Board board)
        => board.Spaces
            .Where(s => s.PropertySet != null)
            .Select(s => new PropertyModel
            {
                Name = s.Name,
                BoardIndex = s.Index,
                
                //Explicit defaults:
                OwnerPlayerId = null,
                State = PropertyState.NotOwned,
                RentLevel = RentLevel.SINGLE,
                StreetRuleQualifier = StreetRuleQualifier.None
            }).ToList();

    public async Task ProcessUnownedProperty(Framework.GameEngine engine, PlayerModel player, 
        BoardSpace space, PropertyModel property, CancellationToken ct)
    {
        //Process Unowned Property Paths:
        //A) RESERVATION: Sends prompt to either RESERVE OR IGNORE
        //B) AFFORD: Sends prompt to BUY or to AUCTION
        //C) SHORTFALL: Sends acknowledge prompt that AUCTION will begin

        if(!space.IsPurchasable || !space.Index.IsProperty(false))
            //No-op - space cannot be purchased or not a property
            return;
        
        if(property.State != PropertyState.NotOwned)
            //No-op - property already owned
            return;
        
        var reservationRule = engine.Cache.Game.ReserveRuleActive;
        var ownedInSet = engine.Cache.Game.GetOwnedProperties(player.PlayerId, space.PropertySet);
        
        //PropSet cannot be null (or shouldn't, since it is a property and purchasable)
        var mustReserve = reservationRule && PropertySetHelper.MustReserve((PropertySet)space.PropertySet!, ownedInSet);
        if(mustReserve)
            //Reservation route (A)
            await ReserveProperty(engine, player, space, property, ct);
        else
            //Normal unowned route (B or C)
            await UnownedProperty(engine, player, space, property, ct);
    }

    private async Task ReserveProperty(Framework.GameEngine engine, PlayerModel player,
        BoardSpace space, PropertyModel property, CancellationToken ct)
    {
        if(space.PurchaseCost is null or 0)
            throw new InvalidOperationException("Purchase cost cannot be null or 0");

        var cost = MoneyHelper.ReservePrice(property.BoardIndex, engine.Cache.Board, engine.Cache.RoundingRule);
        if(cost == 0) throw new InvalidOperationException("Reserve price cannot be 0");

        if (player.Money < cost)
        {
            //Cannot reserve, therefore a no-op, but inform player
            _ = await engine.PromptProvider.Acknowledge(player.PlayerId, "Cannot Reserve",
                $"You do not have enough money to reserve {property.Name} for {RuleDictionary.Currency}{cost}.", ct: ct);
            return;
        }
        
        var response = await engine.PromptProvider.RequestAsync(new AcquirePropertyPrompt
        {
            PlayerId = player.PlayerId,
            Title = $"Reserve {property.Name}",
            Body = $"Would you like to reserve {property.Name} for {RuleDictionary.Currency}{cost}?",
            BoardIndex = space.Index,
            Cost = cost,
            IsReserve = true
        }, ct: ct);
        
        if(!response.Accept)
            //Ignoring, no auction when declining to reserve
            return;

        //Uses same transaction method for purchase (still purchasing the property, just at reservation price)
        await _transactionService.PurchaseProperty(engine, player, cost, space.Index, ct);
        
        //We first OWN the property, then RESERVE it
        property.OwnProperty(player.PlayerId);
        property.ReserveProperty();
        NormaliseRentLevels(engine);
    }

    private async Task UnownedProperty(Framework.GameEngine engine, PlayerModel player,
        BoardSpace space, PropertyModel property, CancellationToken ct)
    {
        if(space.PurchaseCost is null or 0)
            //Throws because this space IS a purchasable property, and MUST have a purchase cost
            throw new InvalidOperationException("Purchase cost cannot be null or 0");

        var cost = (uint)space.PurchaseCost;
        if (property.BoardIndex.IsStation())
        {
            //Owned stations includes mortgaged stations as the player still controls those stations
            //Since those stations are in control of the player, the ownership increase per station applies
            var ownedStationsCount = engine.Cache.Game.GetOwnedProperties(player.PlayerId, PropertySet.Station).Count;
            cost = ownedStationsCount switch
            {
                0 => RuleDictionary.SingleStationCost,
                1 => RuleDictionary.SecondStationCost,
                2 => RuleDictionary.ThirdStationCost,
                3 => RuleDictionary.FourthStationCost,
                _ => throw new InvalidOperationException("Invalid number of owned stations")
            };
        }
        
        cost = MoneyHelper.NormaliseAmount(cost, engine.Cache.RoundingRule, FinancialReason.Purchase);
        var canAfford = player.Money >= cost;
        
        var runAuction = false;
        if (canAfford)
        {
            //Player CAN afford, therefore ask if they want to buy or auction
            var response = await engine.PromptProvider.RequestAsync(new AcquirePropertyPrompt
            {
                PlayerId = player.PlayerId,
                Title = $"Purchase {property.Name}",
                Body = $"Would you like to purchase {property.Name} for {RuleDictionary.Currency}{cost}, or auction it?",
                BoardIndex = property.BoardIndex,
                Cost = cost,
                IsReserve = false
            }, ct: ct);

            //If they do not accept to buy, auction will be held
            if (!response.Accept)
                runAuction = true;
        }
        else
        {
            //Player cannot afford, therefore auction will be held
            runAuction = true;
            _ = await engine.PromptProvider.Acknowledge(player.PlayerId, $"You Cannot Afford {property.Name}",
                $"You do not have enough money to purchase {property.Name} for {RuleDictionary.Currency}{cost}. " +
                "An auction will be held.", ct: ct);
        }
        
        var owningPlayer = player;
        if (runAuction)
        {
            //Auction will be held, therefore run the auction
            var outcome = await _auctionService.RunAuction(engine, property.BoardIndex, ct);
            if(!outcome.Success || outcome.Winner is null)
                //Auction cancelled/failed, therefore a no-op
                return;
                
            //Charge the winning player
            owningPlayer = outcome.Winner;
            await _transactionService.WinAuction(engine, owningPlayer, outcome.Price, property.BoardIndex, ct);
        }
        else
            //No auction, therefore a purchase
            await _transactionService.PurchaseProperty(engine, owningPlayer, cost, property.BoardIndex, ct);
        
        property.OwnProperty(owningPlayer.PlayerId);
        NormaliseRentLevels(engine);
    }
    

    public async Task PayPropertyRent(Framework.GameEngine engine, PlayerModel player, 
        BoardSpace space, PropertyModel property, CancellationToken ct)
    {
        if(!space.IsRentable)
            return;

        var rent = space.GetRent(property.RentLevel);
        if(rent == null) throw new InvalidOperationException("Rent cannot be null for rentable space");
        
        var cost = MoneyHelper.NormaliseAmount((uint)rent, engine.Cache.RoundingRule, FinancialReason.Rent);
        if(cost == 0)
            //No rent, therefore a no-op
            return;
        
        _ = await engine.PromptProvider.Acknowledge(player.PlayerId, $"Rent for {property.Name}",
            $"You owe {RuleDictionary.Currency}{cost} in rent for landing on {property.Name}.", ct: ct);
        
        //Cost is computed in transaction service
        //Cost above is for acknowledge prompt only, shortfall handled in transaction service
        await _transactionService.PayRent(engine, player, property.BoardIndex, ct);
    }


    public void NormaliseRentLevels(Framework.GameEngine engine)
    {
        //This normalises the rent levels for all properties in the game
        var propList = engine.Cache.Board.GetPropertySpaces(engine.Cache.Game.Properties);

        foreach (var (propModel, propSpace) in propList)
        {
            if (propModel.State is PropertyState.NotOwned or PropertyState.FreeParking 
                    or PropertyState.Reserved or PropertyState.Mortgaged 
                || string.IsNullOrEmpty(propModel.OwnerPlayerId))
            {
                //Unowned, FP, reserved, and mortgaged properties are always single-rented
                propModel.RentLevel = RentLevel.SINGLE;
                continue;
            }
                                                                                                                                                       
            var set = propSpace.PropertySet ?? throw new InvalidOperationException("Property space has no property set");  
            
            //Count the number of owned properties in the set (by current property owner), excluding mortgaged and reserved properties
            var owned = engine.Cache.Game.GetOwnedProperties(propModel.OwnerPlayerId!, set, 
                includeMortgaged: false, includeReserved: false).Count;                                                                                  
                                                                                                                                                                                                                                             
            if (set == PropertySet.Station)
                //Stations need their own, since they have unique rent levels
                propModel.RentLevel = owned switch 
                    { 
                        1 => RentLevel.SINGLE, 
                        2 => RentLevel.DOUBLE, 
                        3 => RentLevel.TRIPLE, 
                        4 => RentLevel.SET, 
                        _ => throw new InvalidOperationException($"Invalid owned count for station: {owned}") 
                    };                                                                                                                              
            else if (!propModel.BuiltOn()) 
                //Built on is ignored, rent level for built on properties is never normalised - only modified when buy/sell houses 
                //Works for both buildable properties and utilities (only 2 ulitilies)
                propModel.RentLevel = owned == PropertySetHelper.GetIndexes(set).Count 
                    ? RentLevel.SET 
                    : RentLevel.SINGLE;
        }
    }



    #region Player Command Actions

    public async Task<bool> TryUnReserveProperty(Framework.GameEngine engine, ushort boardIndex, CancellationToken ct)
    {
        //TODO - This will unreserve the property owned by the current player (if unowned, it throws, all other it no-ops)
        return true;
    }

    public async Task<bool> TryMortgageProperty(Framework.GameEngine engine, ushort boardIndex, CancellationToken ct)
    {
        //TODO - This will mortgage the property owned by the current player (if unowned, it throws, all other it no-ops)
        return true;
    }
    
    public async Task<bool> TryUnmortgageProperty(Framework.GameEngine engine, ushort boardIndex, CancellationToken ct)
    {
        //TODO - This will unmortgage the property owned by the current player (if unowned, it throws, all other it no-ops)
        return true;
    }

    public async Task<bool> TryBuildOnProperty(Framework.GameEngine engine, ushort boardIndex, CancellationToken ct)
        => await TryBuildOnProperties(engine, [boardIndex], ct);

    public async Task<bool> TryBuildOnProperties(Framework.GameEngine engine, PropertySet set, CancellationToken ct)
    {
        if (set is PropertySet.Utility or PropertySet.Station)
            return false;
        
        var indexes = PropertySetHelper.GetIndexes(set);
        return await TryBuildOnProperties(engine, indexes, ct);
    }
    
    
    public async Task<bool> TryBuildOnProperties(Framework.GameEngine engine, List<ushort> boardIndexes, CancellationToken ct)
    {
        if(boardIndexes.Any(i => !i.IsProperty()))
            //Any non-buildable properties and return false (cannot build)
            return false;
        
        //TODO - Will add house to each property in list
        //If any properties unowned by current player - it throws
        //If any other validation errors, it no-ops
        return true;
    }
    
    
    public async Task<bool> TrySellOnProperty(Framework.GameEngine engine, ushort boardIndex, CancellationToken ct)
        => await TrySellOnProperties(engine, [boardIndex], ct);

    public async Task<bool> TrySellOnProperties(Framework.GameEngine engine, PropertySet set, CancellationToken ct)
    {
        if (set is PropertySet.Utility or PropertySet.Station)
            return false;
        
        var indexes = PropertySetHelper.GetIndexes(set);
        return await TrySellOnProperties(engine, indexes, ct);
    }
    
    
    public async Task<bool> TrySellOnProperties(Framework.GameEngine engine, List<ushort> boardIndexes, CancellationToken ct)
    {
        if(boardIndexes.Any(i => !i.IsProperty()))
            //Any non-buildable properties and return false (cannot sell house)
            return false;
        
        //TODO - Will remove house from each property in list
        //If any properties unowned by current player - it throws
        //If any other validation errors, it no-ops
        return true;
    }
    

    #endregion
}