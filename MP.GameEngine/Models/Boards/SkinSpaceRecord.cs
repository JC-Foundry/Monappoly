using MP.GameEngine.Enums;
using MP.GameEngine.Enums.Properties;

namespace MP.GameEngine.Models.Boards;

public record SkinSpaceRecord(string Name, ushort Index, BoardSpaceType Type, PropertySet? Set);