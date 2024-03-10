using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Storage.Collection;
using Cs2PracticeModeTests.Helpers.RandomHelperFolder;
using Cs2PracticeModeTests.Helpers.UnitTestFolderFolder;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Storage;

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
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        _ = new LocalStorageCollection<TestDataCollectionClass>(testFolder);

        var idPath = Path.Combine(testFolder, "id");
        var dataFolderPath = Path.Combine(testFolder, "data");

        Assert.True(File.Exists(idPath));
        Assert.True(Directory.Exists(dataFolderPath));
    }

    [Fact]
    public void AddTest()
    {
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<TestDataCollectionClass>(testFolder);

        var testContent = RandomHelper.RandomString();
        var add = localStorage.Add(new TestDataCollectionClass(testContent));

        if (add.IsError)
        {
            Assert.Fail(add.ErrorMessage());
        }

        Assert.False(add.IsError);
        Assert.Equal(add.Value.DataTest, testContent);
        Assert.True(File.Exists(Path.Combine(testFolder, "data", "1.json")));
        Assert.Equal("1", File.ReadAllText(Path.Combine(testFolder, "id")));
    }

    [Fact]
    public void GetAllTest()
    {
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<TestDataCollectionClass>(testFolder);

        var testContent = RandomHelper.RandomString();
        var add = localStorage.Add(new TestDataCollectionClass(testContent));

        if (add.IsError)
        {
            Assert.Fail(add.ErrorMessage());
        }

        Assert.False(add.IsError);
        Assert.Equal(add.Value.DataTest, testContent);
        Assert.True(File.Exists(Path.Combine(testFolder, "data", "1.json")));

        var getAll = localStorage.GetAll();
        if (getAll.IsError)
        {
            Assert.Fail(getAll.ErrorMessage());
        }

        Assert.Single(getAll.Value);
        Assert.Equal(add.Value.Id, getAll.Value.First().Id);
        Assert.Equal(testContent, getAll.Value.First().DataTest);
    }

    [Fact]
    public void GetTest()
    {
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<TestDataCollectionClass>(testFolder);

        var testContent = RandomHelper.RandomString();
        var add = localStorage.Add(new TestDataCollectionClass(testContent));

        if (add.IsError)
        {
            Assert.Fail(add.ErrorMessage());
        }

        Assert.False(add.IsError);
        Assert.Equal(add.Value.DataTest, testContent);
        Assert.True(File.Exists(Path.Combine(testFolder, "data", "1.json")));

        var get = localStorage.Get(1);
        if (get.IsError)
        {
            Assert.Fail(get.ErrorMessage());
        }
        
        Assert.Equal(add.Value.Id, get.Value.Id);
        Assert.Equal(testContent, get.Value.DataTest);
    }
    
    [Fact]
    public void DeleteTest()
    {
        var testFolder = UnitTestFolderHelper.GetNewUnitTestFolder(_outputHelper);
        var localStorage = new LocalStorageCollection<TestDataCollectionClass>(testFolder);

        var testContent = RandomHelper.RandomString();
        var add = localStorage.Add(new TestDataCollectionClass(testContent));

        if (add.IsError)
        {
            Assert.Fail(add.ErrorMessage());
        }

        Assert.False(add.IsError);
        Assert.Equal(add.Value.DataTest, testContent);
        Assert.True(File.Exists(Path.Combine(testFolder, "data", "1.json")));

        var get = localStorage.Get(1);
        if (get.IsError)
        {
            Assert.Fail(get.ErrorMessage());
        }
        
        Assert.Equal(add.Value.Id, get.Value.Id);
        Assert.Equal(testContent, get.Value.DataTest);

        var delete = localStorage.Delete(1);
        if (delete.IsError)
        {
            Assert.Fail(delete.ErrorMessage());
        }
        
        Assert.False(File.Exists(Path.Combine(testFolder, "data", "1.json")));
    }
}