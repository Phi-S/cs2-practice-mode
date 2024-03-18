using System.Data;
using Cs2PracticeMode.Constants;
using Dapper;
using ErrorOr;
using Npgsql;

namespace Cs2PracticeMode.Storage.Single;

public class PostgresStorageSingle<T> : IStorageSingle<T> where T : IData
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public PostgresStorageSingle(string connectionString, string tableName)
    {
        _connectionString = connectionString;
        _tableName = tableName;
        CreateTable();
    }

    private void CreateTable()
    {
        using var connection = new NpgsqlConnection(_connectionString);
        connection.Open();

        var createTableCommand =
            new NpgsqlCommand($"""CREATE TABLE "{_tableName}" ("json" TEXT NOT NULL)""", connection);
        createTableCommand.ExecuteNonQuery();


        var createUniqueIndexCommand =
            new NpgsqlCommand($"""CREATE UNIQUE INDEX one_row_only_uidx ON "{_tableName}" (( TRUE ));""", connection);
        createUniqueIndexCommand.ExecuteNonQuery();

        connection.Close();
    }

    public ErrorOr<T> Get()
    {
        if (Exists() == false)
        {
            return Error.NotFound();
        }

        var sql = $"""SELECT * FROM "{_tableName}" LIMIT 1;""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var dbResult = connection.QuerySingle<string>(sql);
        var data = StorageHelper.Deserialize<T>(dbResult);
        if (data.IsError)
        {
            return data.FirstError;
        }

        return data.Value;
    }

    public ErrorOr<Success> AddOrUpdate(T data)
    {
        var json = StorageHelper.Serialize(data);
        if (json.IsError)
        {
            return json.FirstError;
        }
        using IDbConnection connection = new NpgsqlConnection(_connectionString);

        if (ExistsInternal(connection))
        {
            DeleteInternal(connection);
        }

        var sql = $"""
                   INSERT INTO "{_tableName}" (json)
                   VALUES (@json)
                   """;
        var changedRows = connection.Execute(sql, new { json = json.Value });
        if (changedRows != 1)
        {
            return Errors.Fail("No rows affected");
        }

        return Result.Success;
    }

    public ErrorOr<Deleted> Delete()
    {
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        return DeleteInternal(connection);
    }

    private ErrorOr<Deleted> DeleteInternal(IDbConnection connection)
    {
        var sql = $"""
                   DELETE FROM "{_tableName}"
                   """;
        var changedRows = connection.Execute(sql);
        if (changedRows != 1)
        {
            return Errors.Fail("No rows affected");
        }

        return Result.Deleted;
    }

    public bool Exists()
    {
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        return ExistsInternal(connection);
    }

    private bool ExistsInternal(IDbConnection connection)
    {
        var sql = $"""SELECT COUNT(*) FROM "{_tableName}";""";
        var count = connection.QuerySingle<int>(sql);
        return count == 1;
    }
}