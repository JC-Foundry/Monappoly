namespace MP.GameEngine.Enums.Cards;

/// <summary>Who a card action acts on. Shared across movement, jail, and other actions.</summary>
public enum PlayerTarget
{
    /// <summary>The card holder.</summary>
    Self,
    /// <summary>A player the holder chooses (resolved via a TargetPlayer prompt).</summary>
    ChosenPlayer,
    /// <summary>Every other (non-bankrupt) player.</summary>
    AllOthers
}