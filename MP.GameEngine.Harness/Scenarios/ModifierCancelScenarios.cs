using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Harness.Scenarios;

/// <summary>
/// The roller (Alice) rolls a genuine triple and a bystander (Bob) holds "cancel triple bonus" — on its
/// own, or alongside a payout-modifier card the roller draws on the triple. The cancel fires early in
/// <c>RollTurnDice</c> (OnOtherRollsTriple) while a drawn modifier resolves later in <c>TripleRoll</c>, and
/// <b>both call ApplyTripleBonus</b> (each +£500 accumulator) — so these probe double-increment of the
/// accumulator and cancel precedence. Alice rolls 4 4 4 in every case; Bob targets Alice with the cancel.
/// </summary>
public static class ModifierCancelScenarios
{
    public static IEnumerable<Scenario> All() =>
    [
        new Scenario(1, "Genuine triple + bystander cancel (baseline, #24)",
            "Alice rolls a real triple (her own draw is a neutral Triple card); Bob plays cancel targeting Alice → Alice gets NO payout, accumulator rises to £1500 ONCE. The baseline the modifier combos are compared against. [GitHub #24.]",
            () => ScenarioTable.Base()
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .WithNextCard(CardType.Triple, CardText.ResetJailCost)
                .BuildAsync()),

        new Scenario(2, "No-bonus (drawn) + bystander cancel",
            "Alice draws 'you do not receive your triple bonus' AND Bob cancels → both suppress the payout. Expect NO payout and the accumulator +£500 ONCE (→ £1500). SUSPECT: two ApplyTripleBonus calls → accumulator jumps to £2000.",
            () => ScenarioTable.Base()
                .WithNextCard(CardType.Triple, CardText.NoTripleBonus)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .BuildAsync()),

        new Scenario(3, "Doubled (drawn) + bystander cancel",
            "Alice draws 'your triple bonus is doubled' AND Bob cancels. Expect the cancel to win → NO payout, accumulator +£500 ONCE. SUSPECT: the drawn doubled card still pays £3000 and the accumulator jumps to £2000.",
            () => ScenarioTable.Base()
                .WithNextCard(CardType.Triple, CardText.TripleBonusDoubled)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .BuildAsync()),

        new Scenario(4, "Multiply-×die (drawn) + bystander cancel",
            "Alice draws 'multiply your triple bonus by a rolled die' AND Bob cancels. Expect the cancel to win → NO payout (no die roll needed). SUSPECT: the multiply still runs, prompts a die, and pays.",
            () => ScenarioTable.Base()
                .WithNextCard(CardType.Triple, CardText.TripleBonusMultipliedByDie)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .BuildAsync()),

        new Scenario(5, "Redirect-to-lowest (drawn) + bystander cancel",
            "Alice draws 'lowest roller receives your triple bonus' AND Bob cancels. Expect a single outcome — either the cancel wins (nobody is paid) or the redirect wins (lowest roller paid), never both. Probes precedence.",
            () => ScenarioTable.Base()
                .WithNextCard(CardType.Triple, CardText.TripleBonusToLowestRoller)
                .WithHeldCard("bob", CardType.Third, CardText.CancelTripleBonus)
                .BuildAsync()),
    ];
}
