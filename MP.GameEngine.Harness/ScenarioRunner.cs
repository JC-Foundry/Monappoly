using MP.GameEngine.Models.Snapshot;

namespace MP.GameEngine.Harness;

/// <summary>
/// Shared drive logic for a <see cref="Scenario"/>: build it, print the before-state and the expectation,
/// drive one full turn through the real <c>PlayerTurnOrchestrator</c> (answering prompts via the scenario's
/// console provider), then print the after-state, the rules cited, and every player's hand. All observation
/// — the operator compares the outcome against the scenario's stated expectation.
/// </summary>
public static class ScenarioRunner
{
    public static async Task RunAsync(Scenario scenario, TextWriter output)
    {
        output.WriteLine();
        output.WriteLine($"══════ [{scenario.Id}] {scenario.Name} ══════");
        output.WriteLine($"Expect: {scenario.Expectation}");

        using var game = await scenario.Build();

        PrintState(game.Cache.Game, "BEFORE", output);

        try
        {
            await game.Orchestrator.StartPlayerTurn(game.Engine, CancellationToken.None);
        }
        catch (EndOfStreamException)
        {
            output.WriteLine();
            output.WriteLine("(input stream ended — turn aborted)");
        }
        catch (Exception ex)
        {
            output.WriteLine();
            output.WriteLine($"!! engine threw: {ex.GetType().Name}: {ex.Message}");
            output.WriteLine(ex.StackTrace);
        }

        output.WriteLine();
        output.WriteLine($"Turn state: {game.Cache.TurnState}");
        PrintState(game.Cache.Game, "AFTER", output);
        output.WriteLine("Rules cited: " + string.Join(", ", game.Cache.RuleCodes));
    }

    private static void PrintState(GameModel game, string header, TextWriter output)
    {
        output.WriteLine();
        output.WriteLine($"── {header} (turn {game.Metadata.TurnNumber}, current {game.Metadata.CurrentPlayerId}) ──");
        foreach (var p in game.Players.OrderBy(p => p.OrderId))
            output.WriteLine(
                $"  {p.PlayerId,-6} £{p.Money,-6} idx {p.BoardIndex,-3} {p.Direction,-8} " +
                $"D{p.DoublesInRow}/T{p.TriplesInRow} bonus £{p.TripleBonus} cards {p.CardInstances.Count}" +
                (p.IsInJail ? $" [JAIL {p.JailTurnCounter}]" : ""));
    }
}
