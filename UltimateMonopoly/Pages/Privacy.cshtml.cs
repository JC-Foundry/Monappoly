using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UltimateMonopoly.Pages;

/// <summary>
/// The public privacy policy (/Privacy), linked from the footer. Static content, no handlers beyond the
/// default GET. Public so anonymous visitors can read it before signing up.
/// </summary>
[AllowAnonymous]
public class PrivacyModel : PageModel
{
    public void OnGet()
    {
    }
}