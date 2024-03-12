using Cs2PracticeMode.Storage.Collection;

namespace Cs2PracticeMode.Services._1.AliasStorageFolder;

public class GlobalAliasJsonModel : IDataCollection
{
    public uint Id { get; set; }
    public required string Alias { get; set; }
    public required string Command { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}