using Cs2PracticeMode.Storage;
using Cs2PracticeMode.Storage.Single;
using Cs2PracticeModeTests.Helpers.DockerContainerFolder;
using Cs2PracticeModeTests.Helpers.RandomHelperFolder;
using FluentAssertions;
using FluentAssertions.Execution;
using Npgsql;
using Xunit.Abstractions;

namespace Cs2PracticeModeTests.Storage.Single;

public class PostgresStorageSingleTests
{
    private readonly ITestOutputHelper _outputHelper;

    public PostgresStorageSingleTests(ITestOutputHelper outputHelper)
    {
        _outputHelper = outputHelper;
    }

    [Fact]
    public async Task DefaultTableCreationTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-{RandomHelper.RandomString()}";

        // Act
        _ = new PostgresStorageSingle<SingleTestData>(postgresContainer.connectionString, tableName);

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
    public async Task AddOrUpdateAndGetTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageSingle<SingleTestData>(postgresContainer.connectionString, tableName);

        var testData = new SingleTestData
        {
            TestValue = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.Now
        };

        // Act
        var addOrUpdate = postgresStorage.AddOrUpdate(testData);

        // Assert
        addOrUpdate.IsError.Should().BeFalse();

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();
        var command = new NpgsqlCommand($"""SELECT * FROM "{tableName}" LIMIT 1;""", connection);
        var result = await command.ExecuteScalarAsync();
        await connection.CloseAsync();

        result.Should().BeOfType<string>();
        var json = result as string;
        json.Should().NotBeNullOrEmpty();
        var testDataFromDb = StorageHelper.Deserialize<SingleTestData>(json!);
        testDataFromDb.IsError.Should().BeFalse();
        testDataFromDb.Value.Should().NotBeNull();
        testDataFromDb.Value.TestValue.Should()
            .NotBeNullOrEmpty()
            .And.BeEquivalentTo(testData.TestValue);
        testDataFromDb.Value.UpdatedUtc.Should()
            .Be(testData.UpdatedUtc);
        testDataFromDb.Value.CreatedUtc.Should()
            .Be(testData.CreatedUtc);
    }

    [Fact]
    public async Task DeleteTest()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageSingle<SingleTestData>(postgresContainer.connectionString, tableName);

        var testData = new SingleTestData
        {
            TestValue = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.Now
        };

        var json = StorageHelper.Serialize(testData);
        json.IsError.Should().BeFalse();
        json.Value.Should().NotBeNullOrWhiteSpace();

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();

        var insertCommand = new NpgsqlCommand($"""INSERT INTO "{tableName}" ("json") VALUES ('{json}');""", connection);
        var rowsAffected = await insertCommand.ExecuteNonQueryAsync();
        rowsAffected.Should().Be(1);

        var existsCommand = new NpgsqlCommand($"""SELECT EXISTS (SELECT * FROM "{tableName}");""", connection);
        var existsResult = await existsCommand.ExecuteScalarAsync();
        existsResult.Should().BeOfType<bool>().Subject.Should().BeTrue();

        // Act
        var delete = postgresStorage.Delete();

        // Assert
        delete.IsError.Should().BeFalse();

        existsResult = await existsCommand.ExecuteScalarAsync();
        existsResult.Should().BeOfType<bool>().Subject.Should().BeFalse();
        await connection.CloseAsync();
    }

    [Fact]
    public async Task ExistsTest_False()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageSingle<SingleTestData>(postgresContainer.connectionString, tableName);

        // Act
        var exists = postgresStorage.Exists();

        // Assert
        exists.Should().BeFalse();
    }

    [Fact]
    public async Task ExistsTest_True()
    {
        // Arrange
        var postgresContainer = await PostgresContainer.StartNew(_outputHelper);
        var tableName = $"test-{RandomHelper.RandomString()}";
        var postgresStorage =
            new PostgresStorageSingle<SingleTestData>(postgresContainer.connectionString, tableName);

        var testData = new SingleTestData
        {
            TestValue = RandomHelper.RandomString(),
            UpdatedUtc = DateTime.UtcNow,
            CreatedUtc = DateTime.Now
        };

        var json = StorageHelper.Serialize(testData);
        json.IsError.Should().BeFalse();
        json.Value.Should().NotBeNullOrWhiteSpace();

        await using var connection = new NpgsqlConnection(postgresContainer.connectionString);
        connection.Open();

        var insertCommand = new NpgsqlCommand($"""INSERT INTO "{tableName}" ("json") VALUES ('{json}');""", connection);

        var rowsAffected = await insertCommand.ExecuteNonQueryAsync();
        rowsAffected.Should().Be(1);

        var existsCommand = new NpgsqlCommand($"""SELECT EXISTS (SELECT * FROM "{tableName}");""", connection);
        var existsResult = await existsCommand.ExecuteScalarAsync();
        existsResult.Should().BeOfType<bool>().Subject.Should().BeTrue();
        await connection.CloseAsync();

        // Act
        var exists = postgresStorage.Exists();

        // Assert
        exists.Should().BeTrue();
    }
}