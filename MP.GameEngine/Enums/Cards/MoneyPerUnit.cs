namespace MP.GameEngine.Enums.Cards;

/// <summary>Scales a card's money amount per building/property owned (e.g. "£50 per house").</summary>
public enum MoneyPerUnit
{
    None,
    PerHouse,
    PerHotel,
    PerProperty
}