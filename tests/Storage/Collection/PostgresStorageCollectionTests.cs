using Cs2PracticeMode.Constants;
using Cs2PracticeMode.Storage;
using Cs2PracticeMode.Storage.Collection;
using Cs2PracticeModeTests.Helpers.DockerContainerFolder;
using Cs2PracticeModeTests.Helpers.RandomHelperFolder;
using Dapper;
using FluentAssertions;
using FluentAssertions.Execution;
using Npgsql;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Storage.Collection;

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
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";

        // Act
        _ = new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        // Assert
        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();
        var command = new NpgsqlCommand($"""
                                         SELECT EXISTS (
                                            SELECT 1
                                            FROM pg_tables
                                            WHERE schemaname = 'public'
                                            AND tablename = '{tableName}'
                                         );
                                         """, connection);
        var result = await command.ExecuteScalarAsync();
        await connection.CloseAsync();

        using var __ = new AssertionScope();
        result.Should().BeOfType(typeof(bool)).And.Be(true);
    }

    [Fact]
    public async Task GetAllTest_ShouldBeOne()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        var testData = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testDataJson = StorageHelper.Serialize(testData);
        testDataJson.IsError.Should().BeFalse(testDataJson.ErrorMessage());
        testDataJson.Value.Should().NotBeNullOrWhiteSpace();

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();

        var insertCommand =
            new NpgsqlCommand(
                $"""INSERT INTO "{tableName}" ("id", "json") VALUES ({testData.Id} ,'{testDataJson.Value}');""",
                connection);
        var rowsAffected = await insertCommand.ExecuteNonQueryAsync();
        rowsAffected.Should().Be(1);

        // Act
        var getAll = postgresStorage.GetAll();

        // Assert
        getAll.IsError.Should().BeFalse(getAll.ErrorMessage());
        getAll.Value.Should()
            .HaveCount(1);

        var testDataFromDb = getAll.Value.First();
        testDataFromDb.Id.Should().Be(testData.Id);
        testDataFromDb.DataTest.Should().Be(testData.DataTest);
        testDataFromDb.UpdatedUtc.Should().Be(testData.UpdatedUtc);
        testDataFromDb.CreatedUtc.Should().Be(testData.CreatedUtc);
    }

    [Fact]
    public async Task GetAllTest_ShouldBeTwo()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        var testData1 = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testData2 = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testData1Json = StorageHelper.Serialize(testData1);
        testData1Json.IsError.Should().BeFalse(testData1Json.ErrorMessage());
        testData1Json.Value.Should().NotBeNullOrWhiteSpace();

        var testData2Json = StorageHelper.Serialize(testData2);
        testData2Json.IsError.Should().BeFalse(testData2Json.ErrorMessage());
        testData2Json.Value.Should().NotBeNullOrWhiteSpace();

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();

        var insertTestData1Command =
            new NpgsqlCommand(
                $"""INSERT INTO "{tableName}" ("id", "json") VALUES ({testData1.Id} ,'{testData1Json.Value}');""",
                connection);
        var insertTestData1CommandRowsAffected = await insertTestData1Command.ExecuteNonQueryAsync();
        insertTestData1CommandRowsAffected.Should().Be(1);

        var insertTestData2Command =
            new NpgsqlCommand(
                $"""INSERT INTO "{tableName}" ("id", "json") VALUES ({testData2.Id} ,'{testData2Json.Value}');""",
                connection);
        var insertTestData2CommandRowsAffected = await insertTestData2Command.ExecuteNonQueryAsync();
        insertTestData2CommandRowsAffected.Should().Be(1);

        // Act
        var getAll = postgresStorage.GetAll();

        // Assert
        getAll.IsError.Should().BeFalse(getAll.ErrorMessage());
        getAll.Value.Should()
            .HaveCount(2);

        var testData1FromDb = getAll.Value.First();
        testData1FromDb.Id.Should().Be(testData1.Id);
        testData1FromDb.DataTest.Should().Be(testData1.DataTest);
        testData1FromDb.UpdatedUtc.Should().Be(testData1.UpdatedUtc);
        testData1FromDb.CreatedUtc.Should().Be(testData1.CreatedUtc);

        var testData2FromDb = getAll.Value.Last();
        testData2FromDb.Id.Should().Be(testData2.Id);
        testData2FromDb.DataTest.Should().Be(testData2.DataTest);
        testData2FromDb.UpdatedUtc.Should().Be(testData2.UpdatedUtc);
        testData2FromDb.CreatedUtc.Should().Be(testData2.CreatedUtc);
    }

    [Fact]
    public async Task GetTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        var testData1 = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testData2 = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testData1Json = StorageHelper.Serialize(testData1);
        testData1Json.IsError.Should().BeFalse(testData1Json.ErrorMessage());
        testData1Json.Value.Should().NotBeNullOrWhiteSpace();

        var testData2Json = StorageHelper.Serialize(testData2);
        testData2Json.IsError.Should().BeFalse(testData2Json.ErrorMessage());
        testData2Json.Value.Should().NotBeNullOrWhiteSpace();

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();

        var insertTestData1Command =
            new NpgsqlCommand(
                $"""INSERT INTO "{tableName}" ("id", "json") VALUES ({testData1.Id} ,'{testData1Json.Value}');""",
                connection);
        var insertTestData1CommandRowsAffected = await insertTestData1Command.ExecuteNonQueryAsync();
        insertTestData1CommandRowsAffected.Should().Be(1);

        var insertTestData2Command =
            new NpgsqlCommand(
                $"""INSERT INTO "{tableName}" ("id", "json") VALUES ({testData2.Id} ,'{testData2Json.Value}');""",
                connection);
        var insertTestData2CommandRowsAffected = await insertTestData2Command.ExecuteNonQueryAsync();
        insertTestData2CommandRowsAffected.Should().Be(1);

        // Act
        var get = postgresStorage.Get(testData2.Id);

        // Assert
        get.IsError.Should().BeFalse(get.ErrorMessage());
        get.Value.Id.Should().Be(testData2.Id);
        get.Value.DataTest.Should().Be(testData2.DataTest);
        get.Value.UpdatedUtc.Should().Be(testData2.UpdatedUtc);
        get.Value.CreatedUtc.Should().Be(testData2.CreatedUtc);
    }

    [Fact]
    public async Task AddTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        var testData1 = new CollectionTestData
        {
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testData2 = new CollectionTestData
        {
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        // Act
        var add1 = postgresStorage.Add(testData1);
        var add2 = postgresStorage.Add(testData2);

        // Assert
        add1.IsError.Should().BeFalse(add1.ErrorMessage());
        add1.Value.Id.Should().Be(testData1.Id);
        add1.Value.DataTest.Should().Be(testData1.DataTest);
        add1.Value.UpdatedUtc.Should().Be(testData1.UpdatedUtc);
        add1.Value.CreatedUtc.Should().Be(testData1.CreatedUtc);

        add2.IsError.Should().BeFalse(add2.ErrorMessage());
        add2.Value.Id.Should().Be(testData2.Id);
        add2.Value.DataTest.Should().Be(testData2.DataTest);
        add2.Value.UpdatedUtc.Should().Be(testData2.UpdatedUtc);
        add2.Value.CreatedUtc.Should().Be(testData2.CreatedUtc);

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        var getResult =
            (await connection.QueryAsync<PostgresStorageModel>($"""SELECT * FROM "{tableName}";""")).ToList();
        await connection.CloseAsync();

        getResult.Count.Should().Be(2);

        var testData1JsonFromDb = getResult.First();
        testData1JsonFromDb.Id.Should().Be(testData1.Id);

        var testData1FromDb = StorageHelper.Deserialize<CollectionTestData>(testData1JsonFromDb.Json);
        testData1FromDb.IsError.Should().BeFalse(testData1FromDb.ErrorMessage());
        testData1FromDb.Value.Id.Should().Be(testData1.Id);
        testData1FromDb.Value.DataTest.Should().Be(testData1.DataTest);
        testData1FromDb.Value.UpdatedUtc.Should().Be(testData1.UpdatedUtc);
        testData1FromDb.Value.CreatedUtc.Should().Be(testData1.CreatedUtc);

        var testData2JsonFromDb = getResult.Last();
        testData2JsonFromDb.Id.Should().Be(testData2.Id);
        var testData2FromDb = StorageHelper.Deserialize<CollectionTestData>(testData2JsonFromDb.Json);
        testData2FromDb.IsError.Should().BeFalse(testData2FromDb.ErrorMessage());
        testData2FromDb.Value.Id.Should().Be(testData2.Id);
        testData2FromDb.Value.DataTest.Should().Be(testData2.DataTest);
        testData2FromDb.Value.UpdatedUtc.Should().Be(testData2.UpdatedUtc);
        testData2FromDb.Value.CreatedUtc.Should().Be(testData2.CreatedUtc);
    }

    [Fact]
    public async Task UpdateTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        var testData = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testDataJson = StorageHelper.Serialize(testData);
        testDataJson.IsError.Should().BeFalse(testDataJson.ErrorMessage());

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        var rowsAffected = await connection.ExecuteAsync(
            $"""INSERT INTO "{tableName}" ("id", "json") VALUES (@Id, @Json)""",
            new { id = (int)testData.Id, json = testDataJson.Value });
        rowsAffected.Should().Be(1);

        // Act
        var updatedTestData = new CollectionTestData
        {
            Id = testData.Id,
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.MinValue,
            CreatedUtc = DateTime.MinValue
        };

        var update = postgresStorage.Update(updatedTestData);

        // Assert
        update.IsError.Should().BeFalse(update.ErrorMessage());
        var getResult =
            (await connection.QueryAsync<PostgresStorageModel>($"""SELECT * FROM "{tableName}";""")).ToList();

        getResult.Count.Should().Be(1);
        var storageModel = getResult.First();
        storageModel.Id.Should().Be(testData.Id);
        storageModel.Json.Should().NotBeNullOrWhiteSpace();

        var testDataFromDb = StorageHelper.Deserialize<CollectionTestData>(storageModel.Json);
        testDataFromDb.IsError.Should().BeFalse(testDataFromDb.ErrorMessage());

        testDataFromDb.Value.Id.Should().Be(testData.Id);
        testDataFromDb.Value.UpdatedUtc.Should().NotBe(testData.UpdatedUtc);
        testDataFromDb.Value.CreatedUtc.Should().Be(testData.CreatedUtc);
        testData.DataTest.Should().NotBe(testDataFromDb.Value.DataTest);
    }
    
        [Fact]
    public async Task DeleteTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-table-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageCollection<CollectionTestData>(postgresContainer.connectionString, tableName);

        var testData = new CollectionTestData
        {
            Id = RandomHelper.RandomUInt(),
            DataTest = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.Now,
            CreatedUtc = DateTime.UtcNow
        };

        var testDataJson = StorageHelper.Serialize(testData);
        testDataJson.IsError.Should().BeFalse(testDataJson.ErrorMessage());

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        var rowsAffected = await connection.ExecuteAsync(
            $"""INSERT INTO "{tableName}" ("id", "json") VALUES (@Id, @Json)""",
            new { id = (int)testData.Id, json = testDataJson.Value });
        rowsAffected.Should().Be(1);

        // Act
        var delete = postgresStorage.Delete(testData.Id);

        // Assert
        delete.IsError.Should().BeFalse(delete.ErrorMessage());
        var getResult =
            (await connection.QueryAsync<PostgresStorageModel>($"""SELECT * FROM "{tableName}";""")).ToList();
        getResult.Count.Should().Be(0);
    }
}