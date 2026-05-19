using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Areas.Social.Services;
using UltimateMonopoly.Models.ViewModels.Social;

namespace UltimateMonopoly.Areas.Social.Pages.Friends;

public class IndexModel : PageModel
{
    private readonly FriendService _friendService;

    public IndexModel(FriendService friendService)
    {
        _friendService = friendService;
    }

    public List<FriendViewModel> Friends { get; private set; } = [];
    public IReadOnlyList<FriendRequestViewModel> IncomingRequests { get; private set; } = [];
    public IReadOnlyList<FriendRequestViewModel> OutgoingRequests { get; private set; } = [];

    [BindProperty]
    public AddFriendInput Input { get; set; } = new();

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? StatusKind { get; set; }

    public string Tab { get; private set; } = "friends";

    public async Task OnGetAsync(string? tab = null)
    {
        Tab = NormaliseTab(tab);
        await LoadAsync();
    }

    public async Task<IActionResult> OnPostAddFriendAsync()
    {
        if (!ModelState.IsValid)
        {
            Tab = "add";
            await LoadAsync();
            return Page();
        }

        var username = Input.Username.Trim();
        var result = await _friendService.TrySendFriendRequest(username);

        StatusMessage = result.Success
            ? $"Friend request sent to {username}."
            : result.ErrorMessage ?? "Could not send friend request.";
        StatusKind = result.Success ? "success" : "danger";

        return RedirectToPage(new { tab = "add" });
    }

    public async Task<IActionResult> OnPostAcceptAsync(string requestId)
    {
        var ok = await _friendService.TryAcceptFriendRequest(requestId);
        StatusMessage = ok ? "Friend request accepted." : "Could not accept friend request.";
        StatusKind = ok ? "success" : "danger";
        return RedirectToPage(new { tab = "requests" });
    }

    public async Task<IActionResult> OnPostDeclineAsync(string requestId)
    {
        var ok = await _friendService.TryDeclineFriendRequest(requestId);
        StatusMessage = ok ? "Friend request declined." : "Could not decline friend request.";
        StatusKind = ok ? "success" : "danger";
        return RedirectToPage(new { tab = "requests" });
    }

    private async Task LoadAsync()
    {
        Friends = await _friendService.GetFriendsList();
        var requests = await _friendService.GetFriendRequests();
        IncomingRequests = requests.IncomingRequests;
        OutgoingRequests = requests.OutgoingRequests;
    }

    private static string NormaliseTab(string? tab) => tab switch
    {
        "requests" => "requests",
        "add"      => "add",
        _          => "friends"
    };

    public class AddFriendInput
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Enter a username.")]
        [System.ComponentModel.DataAnnotations.StringLength(64, MinimumLength = 2)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Username")]
        public string Username { get; set; } = "";
    }
}