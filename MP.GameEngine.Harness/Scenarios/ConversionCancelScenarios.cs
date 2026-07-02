using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Harness.Scenarios;

/// <summary>
/// A roll-type conversion (the roller's own downgrade / upgrade) combined with a bystander cancel. These
/// verify the cancel window opens on exactly the rolls it should — never on a downgraded triple, always
/// on an upgraded one.
/// </summary>
public static class ConversionCancelScenarios
{
    public static IEnumerable<Scenario> All() =>
    [
        new Scenario(6, "Downgrade (roller) + bystander cancel (#22)",
            "Alice holds 'downgrade next triple', Bob holds cancel. Alice rolls 4 4 4 → downgraded to a double → NO triple, so Bob's cancel must NOT fire or be consumed. [GitHub #22.]",
            () => ScenarioTable.Base()
                .WithHeldCard("alice", CardType.Third, CardText.DowngradeNextTriple)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .WithNextCard(CardType.Double, CardText.TaxRiseDouble)
                .BuildAsync()),

        new Scenario(7, "Held-convert upgrade (roller) + bystander cancel",
            "Alice holds 'convert double to triple', Bob holds cancel. Alice rolls a double (3 3 5), plays the convert → upgraded triple → Bob is offered the cancel → Alice gets NO payout. Play the convert, then Bob's cancel targeting Alice.",
            () => ScenarioTable.Base()
                .WithHeldCard("alice", CardType.Third, CardText.ConvertDoubleToTriple)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .WithNextCard(CardType.Triple, CardText.ResetJailCost)
                .BuildAsync()),

        new Scenario(8, "Drawn-Double upgrade (roller) + bystander cancel",
            "Alice rolls a double (3 3 5); the drawn Double card upgrades to a triple; Bob holds cancel → Bob is offered the cancel → Alice gets NO payout. Play the cancel targeting Alice.",
            () => ScenarioTable.Base()
                .WithNextCard(CardType.Double, CardText.DoubleConvertedToTriple)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .BuildAsync()),
    ];
}
