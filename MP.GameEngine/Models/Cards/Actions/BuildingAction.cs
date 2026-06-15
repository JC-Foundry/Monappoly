using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Models.Cards.Actions;

/// <summary>
/// A building change driven by a card — currently purging built-on properties, resolved against
/// <c>PurgingService</c>. See <c>design-docs/cards-design.md</c> §3 (Building).
/// </summary>
public sealed class BuildingAction : CardAction
{
    public BuildingKind Kind { get; set; }

    /// <summary>Whose buildings — the holder (<see cref="PlayerTarget.Self"/>) or a chosen player.</summary>
    public PlayerTarget Target { get; set; } = PlayerTarget.Self;

    /// <summary>How many properties to act on (e.g. "purge 2 of your properties").</summary>
    public ushort Count { get; set; } = 1;
}