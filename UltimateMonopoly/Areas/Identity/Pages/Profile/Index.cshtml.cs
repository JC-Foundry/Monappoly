using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Areas.Identity.Services;
using UltimateMonopoly.Data;

namespace UltimateMonopoly.Areas.Identity.Pages.Profile;

public class IndexModel : PageModel
{
    private readonly ProfileService _profile;

    public IndexModel(ProfileService profile)
    {
        _profile = profile;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "image";

    public IReadOnlyList<string> AvailableImageNames { get; private set; } = [];

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? StatusKind { get; set; }

    public async Task OnGetAsync()
    {
        Tab = NormaliseTab(Tab);
        var profile = await _profile.GetAsync();
        Input.AvatarColour = profile.AvatarColour;
        Input.AvatarImageName = profile.AvatarImageName;
        AvailableImageNames = _profile.GetAvailableAvatarImageNames();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Tab = NormaliseTab(Tab);

        var ok = await _profile.TryUpdateAsync(
            new UserProfile(Input.AvatarColour, Input.AvatarImageName));

        StatusMessage = ok ? "Profile updated." : "Could not update profile.";
        StatusKind = ok ? "success" : "danger";

        return RedirectToPage(new { tab = Tab });
    }

    public async Task<IActionResult> OnPostClearColourAsync()
    {
        Tab = NormaliseTab(Tab);

        var ok = await _profile.TryUpdateAsync(
            new UserProfile(null, Input.AvatarImageName));

        StatusMessage = ok ? "Avatar colour cleared." : "Could not clear colour.";
        StatusKind = ok ? "success" : "danger";

        return RedirectToPage(new { tab = Tab });
    }

    public async Task<IActionResult> OnPostClearImageAsync()
    {
        Tab = NormaliseTab(Tab);

        var ok = await _profile.TryUpdateAsync(
            new UserProfile(Input.AvatarColour, null));

        StatusMessage = ok ? "Avatar image cleared." : "Could not clear image.";
        StatusKind = ok ? "success" : "danger";

        return RedirectToPage(new { tab = Tab });
    }

    public IActionResult OnGetAvatarImage(string name)
    {
        var path = _profile.GetAvatarImagePath(name);
        return path is null
            ? NotFound()
            : PhysicalFile(path, "image/png");
    }

    private static string NormaliseTab(string? tab) => tab switch
    {
        "colour" => "colour",
        _ => "image"
    };

    public class InputModel
    {
        public string? AvatarColour { get; set; }
        public string? AvatarImageName { get; set; }
    }
}