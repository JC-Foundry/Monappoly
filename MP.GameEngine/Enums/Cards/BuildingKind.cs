namespace MP.GameEngine.Enums.Cards;

/// <summary>What a <c>BuildingAction</c> does to a player's buildings.</summary>
public enum BuildingKind
{
    /// <summary>Purge N built-on properties (the holder's own, or a chosen player's).</summary>
    Purge,
    /// <summary>Grant a free hotel — bump a chosen four-house property to a hotel (if one is available).</summary>
    GrantHotel
}
