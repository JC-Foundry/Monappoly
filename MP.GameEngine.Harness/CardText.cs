namespace MP.GameEngine.Harness;

/// <summary>
/// Exact <c>RawText</c> of the triple-mechanic cards, kept in one place so scenarios reference cards by a
/// checked constant rather than a loose string literal. These must match the text in
/// <c>config/cards/*.json</c> verbatim — the builder resolves a card by (CardType + text).
/// </summary>
public static class CardText
{
    // ── Roll-type conversion ──
    /// <summary>Double deck, resolve-on-draw — upgrades the current double to a triple.</summary>
    public const string DoubleConvertedToTriple = "Your double is converted into a triple";

    /// <summary>Third deck, held (OnRollDouble) — upgrades a rolled double to a triple.</summary>
    public const string ConvertDoubleToTriple = "Convert a double into a triple. Keep until needed";

    /// <summary>GoToJail deck, held (OnRollDouble + only while in jail) — the "dodgy judge" upgrade.</summary>
    public const string DodgyJudgeDoubleToTripleInJail =
        "Dodgy judge facilitates a double in jail to become a triple (double upgraded to a triple only when in jail). Keep until needed";

    /// <summary>Third deck, held (OnRollTriple) — downgrades the next triple to a double.</summary>
    public const string DowngradeNextTriple = "Your next triple is downgraded to a double";

    // ── Triple-bonus payout modifiers (Triple deck, resolve-on-draw, self) ──
    public const string NoTripleBonus = "You do not receive your triple bonus";
    public const string TripleBonusDoubled = "Your triple bonus is doubled";
    public const string TripleBonusMultipliedByDie = "Roll one die. Multiply your triple bonus by the number rolled";
    public const string TripleBonusToLowestRoller = "Each player rolls one die. The player with the lowest roll receives your triple bonus";

    // ── Cancel (Third deck, held, OnTripleBonus) ──
    public const string CancelTripleBonus = "Cancel a players triple bonus. Keep until needed";

    /// <summary>Free Parking deck, held — the roller plays it reactively to BLOCK a bystander's cancel.</summary>
    public const string ImmunityTripleBonusCancel = "Immunity from triple bonus being cancelled. Keep until needed";

    // ── Jail ──
    /// <summary>Just Visiting deck, resolve-on-draw — swap places with a jailed player; jail leave-fees also swap.</summary>
    public const string JailSwap = "Swap places with any other player in jail. Your jail fees to leave are also swapped.";

    // ── A harmless Triple-deck card to occupy the roller's own triple draw when isolating a cancel. ──
    public const string ResetJailCost = "Your cost to leave jail is reset to £50";

    /// <summary>A Double-deck card with no effect on money/position/roll-type — used to occupy a
    /// double-branch draw (e.g. after a triple is downgraded) without re-upgrading or moving the player.</summary>
    public const string TaxRiseDouble = "Tax Rise! All taxes are doubled until another double is rolled";
}
