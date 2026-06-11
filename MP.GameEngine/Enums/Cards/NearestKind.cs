namespace MP.GameEngine.Enums.Cards;

/// <summary>Target kind for an "advance to the nearest ..." movement.</summary>
public enum NearestKind
{
    Station,
    Utility,
    /// <summary>The nearest coloured (buildable) property.</summary>
    ColourProperty
}