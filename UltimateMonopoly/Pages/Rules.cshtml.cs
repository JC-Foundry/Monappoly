 using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Models.ViewModels;
using UltimateMonopoly.Services;

namespace UltimateMonopoly.Pages;

/// <summary>
/// The public rulebook page (/Rules): the full <see cref="RuleCatalog"/> rendered as a browsable,
/// section-grouped reference. Sidebar sections come from <c>PageRulesHelper</c>; the rules come from
/// <see cref="RuleCatalog.GetRules"/> with no code filter (every rule, citable or not).
/// </summary>
[AllowAnonymous]
public class RulesModel : PageModel
{
    private readonly RuleCatalog _ruleCatalog;

    public RulesModel(RuleCatalog ruleCatalog) => _ruleCatalog = ruleCatalog;

    /// <summary>Every rule in the catalogue, ordered by section → rule → point.</summary>
    public List<GameRule> Rules { get; private set; } = [];

    public async Task OnGetAsync() => Rules = await _ruleCatalog.GetRules();
}