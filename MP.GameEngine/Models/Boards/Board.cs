namespace MP.GameEngine.Models.Boards;

public class Board(string name, List<BoardSpace> spaces, string? skinId = null)
{
    public string? SkinId { get; } = skinId;
    public string Name { get; } = name;
    public List<BoardSpace> Spaces { get; } = spaces;
}