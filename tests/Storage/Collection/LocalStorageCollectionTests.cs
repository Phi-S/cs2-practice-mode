using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Storage;
using Cs2PracticeMode.Storage.Collection;
using Cs2PracticeModeTests.Helpers.RandomHelperFolder;
using Cs2PracticeModeTests.Helpers.UnitTestFolderFolder;
using FluentAssertions;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Storage.Collection;

public class LocalStorageCollectionTests
{
    private readonly ITestOutputHelper _outputHelper;

    public LocalStorageCollectionTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public void DefaultFileCreationTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);

        // Act
        _ = new LocalStorageCollection<CollectionTestData>(testFolder);

        // Assert
        var idPath = Path.Combine(testFolder, "id");
        var dataFolderPath = Path.Combine(testFolder, "data");

        File.Exists(idPath).Should().BeTrue();
        Directory.Exists(dataFolderPath).Should().BeTrue();
    }

    [Fact]
    public void AddTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<CollectionTestData>(testFolder);

        var testContent = RandomHelper.RandomString();
        var testData = new CollectionTestData
        {
            DataTest = testContent
        };

        // Act
        var add = localStorage.Add(testData);

        // Assert
        add.IsError.Should().BeFalse(add.ErrorMessage());
        add.Value.DataTest.Should().Be(testContent);
        File.Exists(Path.Combine(testFolder, "data", "1.json")).Should().BeTrue();
        File.ReadAllText(Path.Combine(testFolder, "id")).Should().Be("1");
    }

    [Fact]
    public void GetAllTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<CollectionTestData>(testFolder);

        var testContent = RandomHelper.RandomString();
        var testData = new CollectionTestData
        {
            DataTest = testContent
        };

        var add = localStorage.Add(testData);
        add.IsError.Should().BeFalse(add.ErrorMessage());
        add.Value.DataTest.Should().Be(testContent);
        File.Exists(Path.Combine(testFolder, "data", "1.json")).Should().BeTrue();

        // Act
        var getAll = localStorage.GetAll();

        // Assert
        getAll.IsError.Should().BeFalse(getAll.ErrorMessage());
        getAll.Value.Should().HaveCount(1);
        getAll.Value.First().Id.Should().Be(add.Value.Id);
        getAll.Value.First().DataTest.Should().Be(add.Value.DataTest);
    }

    [Fact]
    public void GetTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<CollectionTestData>(testFolder);

        var testContent = RandomHelper.RandomString();
        var testData = new CollectionTestData
        {
            DataTest = testContent
        };

        // Act
        var add = localStorage.Add(testData);

        // Assert
        add.IsError.Should().BeFalse(add.ErrorMessage());
        add.Value.DataTest.Should().Be(testContent);

        var jsonPath = Path.Combine(testFolder, "data", "1.json");
        File.Exists(jsonPath).Should().BeTrue();
        var json = File.ReadAllText(jsonPath);
        json.Should().NotBeNullOrWhiteSpace();

        var testDataFromFile = StorageHelper.Deserialize<CollectionTestData>(json);
        testDataFromFile.IsError.Should().BeFalse(testDataFromFile.ErrorMessage());
        testDataFromFile.Value.Id.Should().Be(testData.Id);
        testDataFromFile.Value.DataTest.Should().Be(testData.DataTest);
    }

    [Fact]
    public void DeleteTest()
    {
        // Arrange
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<CollectionTestData>(testFolder);

        var testData = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var json = StorageHelper.Serialize(testData);
        json.IsError.Should().BeFalse(json.ErrorMessage());
        
        var jsonPath = Path.Combine(testFolder, "data", $"{testData.Id}.json");
        File.WriteAllText(jsonPath, json.Value);

        // Act
        var delete = localStorage.Delete(testData.Id);

        // Assert
        delete.IsError.Should().BeFalse(delete.ErrorMessage());
        File.Exists(jsonPath).Should().BeFalse();
    }
}