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

    public IndexModel(BoardSkinService boardSkins)
    {
        _boardSkins = boardSkins;
    }

    public List<BoardSkinViewModel> Skins { get; private set; } = [];

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? StatusKind { get; set; }

    public async Task OnGetAsync()
    {
        Skins = await _boardSkins.GetAllBoardSkins();
    }
}
