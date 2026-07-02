using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Harness.Scenarios;

/// <summary>Remaining combinations: a conversion feeding a payout modifier, and the roller blocking a cancel with immunity.</summary>
public static class MiscComboScenarios
{
    public static IEnumerable<Scenario> All() =>
    [
        new Scenario(9, "Held-convert upgrade + doubled bonus (drawn)",
            "Alice holds 'convert double to triple' and rolls a double (3 3 5) → upgraded to a triple → the drawn Triple card 'doubled' pays 2×£1000 = £2000; accumulator → £1500. Play the convert. Checks an upgraded triple still feeds a drawn payout modifier correctly.",
            () => ScenarioTable.Base()
                .WithHeldCard("alice", CardType.Third, CardText.ConvertDoubleToTriple)
                .WithNextCard(CardType.Triple, CardText.TripleBonusDoubled)
                .BuildAsync()),

        new Scenario(10, "Immunity vs cancel — roller blocks the cancel",
            "Alice (roller) holds 'immunity from triple bonus being cancelled'; Bob holds cancel. Alice rolls a real triple → Bob plays cancel targeting Alice → Alice is offered her immunity and plays it → the cancel is BLOCKED → Alice is PAID the full £1000, accumulator → £1500. Play Bob's cancel, target Alice, then play Alice's immunity.",
            () => ScenarioTable.Base()
                .WithHeldCard("alice", CardType.FreeParking, CardText.ImmunityTripleBonusCancel)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .WithNextCard(CardType.Triple, CardText.ResetJailCost)
                .BuildAsync()),
    ];
}
