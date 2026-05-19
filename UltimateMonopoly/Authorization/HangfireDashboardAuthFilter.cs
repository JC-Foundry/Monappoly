using Hangfire.Dashboard;
using JC.Identity.Authentication;

namespace UltimateMonopoly.Authorization;

public class HangfireDashboardAuthFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
               && httpContext.User.IsInRole(SystemRoles.SystemAdmin);
    }
}
