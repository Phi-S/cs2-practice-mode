using Cs2PracticeMode.Storage.Collection;

namespace Cs2PracticeModeTests.Storage;

public class TestDataCollectionClass : IDataCollection
{
    public TestDataCollectionClass(string dataTest)
    {
        DataTest = dataTest;
    }

    public uint Id { get; set; }
    public string DataTest { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}