using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace UltimateMonopoly.Pages;

/// <summary>
/// The public Terms of Service (/Terms), linked from the footer. Static content, no handlers beyond the
/// default GET. Public so anonymous visitors can read it before signing up.
/// </summary>
[AllowAnonymous]
public class TermsModel : PageModel
{
    public void OnGet()
    {
    }
}