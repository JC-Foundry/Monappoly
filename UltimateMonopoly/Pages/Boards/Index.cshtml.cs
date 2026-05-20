using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Models.ViewModels.BoardSkins;
using UltimateMonopoly.Services.BoardSkins;

namespace UltimateMonopoly.Pages.Boards;

[Authorize]
public class IndexModel : PageModel
{
    private readonly BoardSkinService _boardSkins;
    private readonly BoardSkinShareService _shareService;

    public IndexModel(BoardSkinService boardSkins, BoardSkinShareService shareService)
    {
        _boardSkins = boardSkins;
        _shareService = shareService;
    }

    public List<BoardSkinViewModel> Skins { get; private set; } = [];
    public List<BoardSkinViewModel> SharedSkins { get; private set; } = [];

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? StatusKind { get; set; }

    public async Task OnGetAsync()
    {
        Skins = await _boardSkins.GetAllBoardSkins();
        SharedSkins = await _shareService.GetSharedBoardSkins();
    }

    public async Task<IActionResult> OnPostRemoveShareAsync(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return RedirectToPage();

        var ok = await _shareService.TryRemoveSharedBoardSkin(id);
        StatusMessage = ok ? "Shared board removed." : "Could not remove shared board.";
        StatusKind = ok ? "success" : "danger";
        return RedirectToPage();
    }
}
