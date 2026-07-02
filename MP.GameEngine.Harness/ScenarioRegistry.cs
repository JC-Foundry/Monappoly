using MP.GameEngine.Harness.Scenarios;

namespace MP.GameEngine.Harness;

/// <summary>
/// The catalogue of all harness scenarios, aggregated from the per-theme scenario files and ordered by id.
/// Add a scenario by adding it to its theme file (or a new one) and wiring that file's <c>All()</c> in here.
/// </summary>
public static class ScenarioRegistry
{
    private static readonly IReadOnlyList<Scenario> _all =
        ModifierCancelScenarios.All()
            .Concat(ConversionCancelScenarios.All())
            .Concat(MiscComboScenarios.All())
            .OrderBy(s => s.Id)
            .ToList();

    public static IReadOnlyList<Scenario> All() => _all;

    public static Scenario? Find(int id) => _all.FirstOrDefault(s => s.Id == id);
}
