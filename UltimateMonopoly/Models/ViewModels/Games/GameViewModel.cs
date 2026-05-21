using JC.Core.Extensions;
using UltimateMonopoly.Models.DataModels.Games;

namespace UltimateMonopoly.Models.ViewModels.Games;

public class GameViewModel
{
    public string Id { get; }
    public string JoinCode { get; }
    public string Name { get; }
    public string State { get; }
    public string Outcome { get; }
    public string RoundingRule { get; }
    
    public string CreatedDate { get; }
    public bool IsHost { get; }

    public IReadOnlyList<GamePlayerViewModel> Players { get; } = [];
    public IReadOnlyList<GameTurnViewModel> Turns { get; } = [];

    public GameViewModel(Game game, string? userId = null)
    {
        Id = game.Id;
        JoinCode = game.JoinCode;
        Name = game.Name;
        State = game.State.ToDisplayName();
        Outcome = game.Outcome.ToDisplayName();
        RoundingRule = game.RoundingRule.GetDescription();

        CreatedDate = game.CreatedUtc.ToLocalTime().ToString("dd-MMMM yyyy HH:mm");
        IsHost = userId != null && game.CreatedById == userId;

        if (game.Players != null!)
            Players = game.Players
                .Select(p => new GamePlayerViewModel(p, userId == p.UserId))
                .ToList();
        
        if (game.Turns != null!)
            Turns = game.Turns
                .Select(t => new GameTurnViewModel(t))
                .ToList();
    }
}