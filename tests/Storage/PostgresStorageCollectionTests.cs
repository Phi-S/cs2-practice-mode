using Cs2PracticeMode.Storage.Collection;
using Cs2PracticeModeTests.Helpers.DockerContainerFolder;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Storage;

public class PostgresStorageCollectionTests
{
    private readonly ITestOutputHelper _outputHelper;

    public PostgresStorageCollectionTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }
    
    [Fact]
    public async Task DefaultTableCreationTest()
    {
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var postgresStorage = new PostgresStorageCollection<TestDataCollectionClass>(postgresContainer.connectionString, "test-data");
        
        
    }
}