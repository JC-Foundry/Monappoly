using JC.Core.Extensions;
using UltimateMonopoly.Models.DataModels.Boards;

namespace UltimateMonopoly.Models.ViewModels.BoardSkins;

public class BoardSkinSpaceViewModel
{
    public string Id { get; }
    public string BoardId { get; }
    
    public string Name { get; }
    
    public ushort Index { get; }
    public string SpaceType { get; }
    public string? PropertyColour { get; }

    public BoardSkinSpaceViewModel(BoardSkinSpace boardSkinSpace)
    {
        Id = boardSkinSpace.Id;
        BoardId = boardSkinSpace.BoardId;
        Name = boardSkinSpace.Name;
        Index = boardSkinSpace.Index;
        SpaceType = boardSkinSpace.SpaceType.ToDisplayName();
        PropertyColour = boardSkinSpace.PropertyColour?.ToDisplayName();
    }
}