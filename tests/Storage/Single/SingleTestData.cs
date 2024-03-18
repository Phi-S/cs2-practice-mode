using Cs2PracticeMode.Storage.Single;

namespace Cs2PracticeModeTests.Storage.Single;

public class SingleTestData : IData
{
    public required string TestValue { get; set; }
    public DateTime UpdatedUtc { get; set; }
    public DateTime CreatedUtc { get; set; }
}