using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Models.ViewModels.Games;
using UltimateMonopoly.Services.Games;

namespace UltimateMonopoly.Pages.Games;

public class MyGamesModel : PageModel
{
    private readonly GameListService _gameList;

    public MyGamesModel(GameListService gameList)
    {
        _gameList = gameList;
    }

    public List<GameViewModel> Games { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Games = await _gameList.GetAllMyGames();
    }

    public IActionResult OnPostDelete(string gameId)
    {
        // TODO: soft-delete the game — backend not yet implemented.
        return RedirectToPage();
    }
}