using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using UltimateMonopoly.Models.ViewModels.Games;
using UltimateMonopoly.Services.Games;

namespace UltimateMonopoly.Pages.Games;

public class JoinedModel : PageModel
{
    private readonly GameListService _gameList;

    public JoinedModel(GameListService gameList)
    {
        _gameList = gameList;
    }

    public List<GameViewModel> Games { get; private set; } = [];

    public async Task OnGetAsync()
    {
        Games = await _gameList.GetAllGamesJoined();
    }
}