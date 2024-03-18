using Cs2PracticeMode.Storage.Collection;

namespace Cs2PracticeModeTests.Storage.Collection;

public class CollectionTestData : IDataCollection
{
    public uint Id { get; set; }
    public required string DataTest { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}