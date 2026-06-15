using MP.GameEngine.Abstractions.Cards;
using MP.GameEngine.Enums.Cards;
using MP.GameEngine.Enums.Properties;
using MP.GameEngine.Models.Cards.Actions;
using MP.GameEngine.Models.Prompts.PromptTypes;
using MP.GameEngine.Models.Snapshot;
using MP.GameEngine.Services.SubSystems;

namespace MP.GameEngine.Services.Cards.Actions;

/// <summary>
/// Resolves a card <see cref="BuildingAction"/> — purging built-on properties (via
/// <see cref="PurgingService"/>) or granting a free hotel (bumping a chosen four-house property to a
/// hotel when one is available, then re-normalising via <see cref="PropertyService"/>). See
/// cards-design.md §3 (Building).
/// </summary>
public class BuildingActionService : ICardActionService<BuildingAction>
{
    private readonly PurgingService _purgingService;
    private readonly PropertyService _propertyService;

    /// <summary>Creates the building-action handler over the purge and property-normalisation seams.</summary>
    public BuildingActionService(PurgingService purgingService, PropertyService propertyService)
    {
        _purgingService = purgingService;
        _propertyService = propertyService;
    }

    /// <summary>Applies the building action.</summary>
    public async Task ResolveActionAsync(Framework.GameEngine engine, PlayerModel player, BuildingAction action, CancellationToken ct)
    {
        switch (action.Kind)
        {
            case BuildingKind.Purge:
                if (action.Target == PlayerTarget.ChosenPlayer)
                    await _purgingService.PurgeOthersProperty(engine, player, action.Count, ct);
                else
                    await _purgingService.PurgeOwnProperty(engine, player, action.Count, ct);
                break;

            case BuildingKind.GrantHotel:
                await GrantHotel(engine, player, ct);
                break;
        }
    }

    /// <summary>
    /// Grants a free hotel: no-op when no hotel is available or the holder has no property ready for
    /// one (a complete street built up to four houses). Otherwise the holder chooses which
    /// four-house property becomes a hotel, free of charge.
    /// </summary>
    private async Task GrantHotel(Framework.GameEngine engine, PlayerModel player, CancellationToken ct)
    {
        var (_, hotelsLeft) = engine.Cache.Game.GetHousesAndHotelsLeft();
        if (hotelsLeft == 0)
            return;

        var eligible = engine.Cache.Game.GetOwnedProperties(player.PlayerId)
            .Where(p => p.RentLevel == RentLevel.FOUR_HOUSES)
            .ToList();
        if (eligible.Count == 0)
            return;

        var response = await engine.PromptProvider.RequestAsync(new TargetPropertyPrompt
        {
            PlayerId = player.PlayerId,
            Title = "Free Hotel",
            Body = "Choose a property to receive a free hotel.",
            EligibleBoardIndexes = eligible.Select(p => p.BoardIndex).ToList(),
            Count = 1
        }, ct);

        if (response.SelectedBoardIndexes.Count == 0)
            return;

        var property = engine.Cache.Game.GetPropertySpace(response.SelectedBoardIndexes[0]);
        if (property is null || property.RentLevel != RentLevel.FOUR_HOUSES)
            return;

        property.RentLevel = RentLevel.HOTEL;
        _propertyService.NormaliseProperties(engine);
    }
}
