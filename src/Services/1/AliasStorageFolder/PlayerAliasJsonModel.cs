using Cs2PracticeMode.Storage.Collection;

namespace Cs2PracticeMode.Services._1.AliasStorageFolder;

public class PlayerAliasJsonModel : IDataCollection
{
    public uint Id { get; set; }
    public ulong PlayerSteamId { get; init; }
    public required string Alias { get; init; }
    public required string Command { get; init; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}