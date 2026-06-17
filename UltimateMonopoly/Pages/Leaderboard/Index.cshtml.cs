using JC.Core.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Models.ViewModels.Leaderboard;
using UltimateMonopoly.Services.Statistics;

namespace UltimateMonopoly.Pages.Leaderboard;

/// <summary>
/// The leaderboard page (/Leaderboard): every player ranked best-first by
/// <see cref="LeaderboardService.GetLeaderboard"/>. The view renders the top three as a podium
/// (gold / silver / bronze) and the rest as a ranked list.
/// </summary>
public class IndexModel : PageModel
{
    public const int MinimumGames = 1;
    
    private readonly LeaderboardService _leaderboardService;
    private readonly IUserInfo _userInfo;

    public IndexModel(LeaderboardService leaderboardService, IUserInfo userInfo)
    {
        _leaderboardService = leaderboardService;
        _userInfo = userInfo;
    }

    /// <summary>Players ranked best-first (index 0 = #1).</summary>
    public List<LeaderboardRecord> Records { get; private set; } = [];

    /// <summary>The viewing player's id, used to highlight their own row.</summary>
    public string? CurrentUserId { get; private set; }
    
    public bool ShowLeaderboard { get; private set; }

    public async Task OnGetAsync()
    {
        Records = await _leaderboardService.GetLeaderboard();
        CurrentUserId = _userInfo.UserId;
        
        if(Records.Select(r => r.UserProfile.UserId).Contains(CurrentUserId))
            ShowLeaderboard = true;
    }
}