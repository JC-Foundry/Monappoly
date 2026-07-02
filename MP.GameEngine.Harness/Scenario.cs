namespace MP.GameEngine.Harness;

/// <summary>
/// One playable scenario in the harness — an identified starting state (mid-game position + pinned /
/// seeded cards) the operator drives a turn through to observe a specific card interaction. Held as data
/// (id + metadata + a build delegate) so scenarios are declared concisely in the catalogue files rather
/// than a class each.
/// </summary>
/// <param name="Id">Menu id, stable so a scenario can be referenced across runs.</param>
/// <param name="Name">Short title shown in the menu.</param>
/// <param name="Expectation">What correct behaviour looks like — the operator checks the run against this.</param>
/// <param name="Build">Builds the scenario's state + wired engine (console prompt provider by default).</param>
public sealed record Scenario(int Id, string Name, string Expectation, Func<Task<GameScenario>> Build);
