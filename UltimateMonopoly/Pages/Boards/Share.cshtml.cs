using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Areas.Social.Services;
using UltimateMonopoly.Models.ViewModels.BoardSkins;
using UltimateMonopoly.Models.ViewModels.Social;
using UltimateMonopoly.Services.BoardSkins;

namespace UltimateMonopoly.Pages.Boards;

[Authorize]
public class Share : PageModel
{
    private readonly BoardSkinService _boardSkins;
    private readonly BoardSkinShareService _shareService;
    private readonly FriendService _friendService;

    public Share(BoardSkinService boardSkins,
        BoardSkinShareService shareService,
        FriendService friendService)
    {
        _boardSkins = boardSkins;
        _shareService = shareService;
        _friendService = friendService;
    }

    public string Id { get; private set; } = string.Empty;
    public BoardSkinViewModel? Skin { get; private set; }
    public List<FriendViewModel> Friends { get; private set; } = [];
    public HashSet<string> SharedFriendIds { get; private set; } = [];

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? StatusKind { get; set; }

    public async Task<IActionResult> OnGetAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        Skin = await _boardSkins.GetBoardSkin(id, includeSpaces: false);
        if (Skin is null) return NotFound();

        Id = id;
        Friends = await _friendService.GetFriendsList();
        SharedFriendIds = (await _shareService.GetUserIdsForSharedBoardSkin(id)).ToHashSet();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string? id, List<string>? friendIds)
    {
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var skin = await _boardSkins.GetBoardSkin(id, includeSpaces: false);
        if (skin is null) return NotFound();

        var ok = await _shareService.TryShareBoardSkin(id, friendIds ?? []);

        StatusMessage = ok ? "Board sharing saved." : "Could not save board sharing.";
        StatusKind = ok ? "success" : "danger";
        return RedirectToPage(new { id });
    }
}