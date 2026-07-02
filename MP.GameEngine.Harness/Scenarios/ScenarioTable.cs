namespace MP.GameEngine.Harness.Scenarios;

/// <summary>
/// The shared mid-game table the triple scenarios build on: three players with unique dice numbers so no
/// roll accidentally triggers the dice-number bonus. <b>Alice</b> is the current player (the roller);
/// <b>Bob</b> and <b>Carol</b> are bystanders. Each scenario starts from this and layers on the cards it
/// needs (pinned next-draws and/or seeded hands).
/// </summary>
public static class ScenarioTable
{
    public static GameScenarioBuilder Base(bool aliceInJail = false)
        => GameScenarioBuilder.Create()
            .WithTurnNumber(12)
            .WithCurrentPlayer("alice")
            .WithPlayer("alice", p =>
            {
                p.Dice(2, 5).WithMoney(1800);
                if (aliceInJail) p.InJail(1);
                else p.At(24);
            })
            .WithPlayer("bob", p => p.Dice(3, 6).WithMoney(1200).At(11))
            .WithPlayer("carol", p => p.Dice(1, 4).WithMoney(900).At(31));
}
