using MP.GameEngine.Harness;

// Anchor the working directory to the harness project folder so the relative config paths in
// CardCacheMock / TestHarness (..\config\...) resolve to the repo's config regardless of where the
// process is launched from. BaseDirectory is bin/<cfg>/net9.0 → up three is the project directory.
Directory.SetCurrentDirectory(Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..")));

var output = Console.Out;

while (true)
{
    PrintMenu(output);
    output.Write("Scenario id (blank to quit): ");

    var line = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(line)) break;

    if (!int.TryParse(line.Trim(), out var id) || ScenarioRegistry.Find(id) is not { } scenario)
    {
        output.WriteLine($"  ! no scenario with id '{line.Trim()}'.");
        continue;
    }

    await ScenarioRunner.RunAsync(scenario, output);
    output.WriteLine();
    output.WriteLine(new string('─', 60));
}

return;

static void PrintMenu(TextWriter output)
{
    output.WriteLine();
    output.WriteLine("=== MP.GameEngine — triple-card scenario harness ===");
    foreach (var s in ScenarioRegistry.All())
        output.WriteLine($"  [{s.Id,2}] {s.Name}");
}
