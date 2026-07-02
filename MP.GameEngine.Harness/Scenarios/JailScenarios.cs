using MP.GameEngine.Enums.Cards;

namespace MP.GameEngine.Harness.Scenarios;

/// <summary>Jail-card combinations. Currently the "swap places with a jailed player, fees also swapped" card (#23).</summary>
public static class JailScenarios
{
    public static IEnumerable<Scenario> All() =>
    [
        new Scenario(11, "Jail-swap card — swaps places AND leave-fees (#23)",
            "Bob is in jail with an escalated £113 leave-fee (Alice's is the default £50). Alice lands on Just Visiting and draws 'swap places with a jailed player, fees also swapped'; she picks Bob → Alice takes Bob's jail spot and Bob comes out. EXPECT the leave-fees to swap: Alice £113, Bob £50. Roll 2 3 1 (moves 5 → Just Visiting), then pick Bob. [GitHub #23: the fee did not swap.]",
            () => GameScenarioBuilder.Create()
                .WithTurnNumber(12)
                .WithCurrentPlayer("alice")
                .WithPlayer("alice", p => p.Dice(2, 5).WithMoney(1500).At(5))
                .WithPlayer("bob", p =>
                {
                    p.Dice(3, 6).WithMoney(1200).InJail(2);
                    p.JailCost = 113;
                })
                .WithPlayer("carol", p => p.Dice(1, 4).WithMoney(900).At(31))
                .WithNextCard(CardType.JustVisiting, CardText.JailSwap)
                .BuildAsync()),
    ];
}
