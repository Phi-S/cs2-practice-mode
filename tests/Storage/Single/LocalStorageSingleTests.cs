using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Storage;
using Cs2PracticeMode.Storage.Single;
using Cs2PracticeModeTests.Helpers.RandomHelperFolder;
using Cs2PracticeModeTests.Helpers.UnitTestFolderFolder;
using FluentAssertions;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Storage.Single;

public class LocalStorageSingleTests
{
    private readonly ITestOutputHelper _outputHelper;

    public LocalStorageSingleTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void GetTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var storageName = $"test-storage-{RandomHelper.RandomString()}";
        var storage = new LocalStorageSingle<SingleTestData>(testFolder, storageName);

        var testData = new SingleTestData
        {
            TestValue = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var json = StorageHelper.Serialize(testData);
        json.IsError.Should().BeFalse(json.ErrorMessage());

        var jsonPath = Path.Combine(testFolder, storageName);
        File.WriteAllText(jsonPath, json.Value);
        
        // Act
        var get = storage.Get();

        // Assert
        get.IsError.Should().BeFalse(get.ErrorMessage());
        get.Value.TestValue.Should().Be(testData.TestValue);
        get.Value.UpdatedUtc.Should().Be(testData.UpdatedUtc);
        get.Value.CreatedUtc.Should().Be(testData.CreatedUtc);
    }

    [Fact]
    public void AddOrUpdateTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var storageName = $"test-storage-{RandomHelper.RandomString()}";
        var storage = new LocalStorageSingle<SingleTestData>(testFolder, storageName);

        var testData = new SingleTestData
        {
            TestValue = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };
        
        // Act
        var addOrUpdate = storage.AddOrUpdate(testData);

        // Assert
        addOrUpdate.IsError.Should().BeFalse(addOrUpdate.ErrorMessage());
        var jsonPath = Path.Combine(testFolder, storageName);
        var json = File.ReadAllText(jsonPath);
        var testDataFromStorage = StorageHelper.Deserialize<SingleTestData>(json);
        testDataFromStorage.IsError.Should().BeFalse(testDataFromStorage.ErrorMessage());
        testDataFromStorage.Value.TestValue.Should().Be(testData.TestValue);
        testDataFromStorage.Value.UpdatedUtc.Should().Be(testData.UpdatedUtc);
        testDataFromStorage.Value.CreatedUtc.Should().Be(testData.CreatedUtc);
    }

    [Fact]
    public void DeleteTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var storageName = $"test-storage-{RandomHelper.RandomString()}";
        var storage = new LocalStorageSingle<SingleTestData>(testFolder, storageName);

        var testData = new SingleTestData
        {
            TestValue = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };
        
        var json = StorageHelper.Serialize(testData);
        json.IsError.Should().BeFalse(json.ErrorMessage());

        var jsonPath = Path.Combine(testFolder, storageName);
        File.WriteAllText(jsonPath, json.Value);
        
        // Act
        var delete = storage.Delete();

        // Assert
        delete.IsError.Should().BeFalse(delete.ErrorMessage());
        File.Exists(jsonPath).Should().BeFalse();
    }
}