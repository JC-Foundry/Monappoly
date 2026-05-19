using UltimateMonopoly.Models.DataModels.Boards;

namespace UltimateMonopoly.Models.ViewModels.BoardSkins;

public class BoardSkinViewModel
{
    public string Id { get; }
    public string Name { get; }
    public string? Description { get; }

    public IReadOnlyList<BoardSkinSpaceViewModel> Spaces { get; set; } = [];

    public BoardSkinViewModel(BoardSkin boardSkin)
    {
        Id = boardSkin.Id;
        Name = boardSkin.Name;
        Description = boardSkin.Description;
        
        if(boardSkin.Spaces != null!)
            Spaces = boardSkin.Spaces.Select(x => new BoardSkinSpaceViewModel(x)).ToList();
    }
}