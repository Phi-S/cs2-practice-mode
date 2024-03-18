using System.Data;
using Cs2PracticeMode.Constants;
using Dapper;
using ErrorOr;
using Npgsql;

namespace Cs2PracticeMode.Storage.Collection;

public class PostgresStorageModel
{
    public required uint Id { get; init; }
    public required string Json { get; init; }
}

public class PostgresStorageCollection<T> : IStorageCollection<T> where T : IDataCollection
{
    private readonly string _connectionString;
    private readonly string _tableName;

    public PostgresStorageCollection(string connectionString, string tableName)
    {
        _connectionString = connectionString;
        _tableName = tableName;
        CreateTable();
    }

    private void CreateTable()
    {
        var connection = new NpgsqlConnection(_connectionString);
        var command =
            new NpgsqlCommand($"""
                               CREATE TABLE "{_tableName}" (
                               id SERIAL PRIMARY KEY,
                               json TEXT NOT NULL);
                               """, connection);
        connection.Open();
        command.ExecuteNonQuery();
        connection.Close();
    }

    public ErrorOr<List<T>> GetAll()
    {
        var sql = $"""SELECT * FROM "{_tableName}";""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var result = new List<T>();
        var dbResults = connection.Query<PostgresStorageModel>(sql);
        foreach (var dbResult in dbResults)
        {
            var data = StorageHelper.Deserialize<T>(dbResult.Json);
            if (data.IsError)
            {
                return data.FirstError;
            }

            var json = data.Value;

            if (dbResult.Id != json.Id)
            {
                return Errors.Fail($"Database id \"{dbResult.Id}\" does not match the json id \"{json.Id}\"");
            }

            result.Add(data.Value);
        }

        return result;
    }

    public ErrorOr<T> Get(uint id)
    {
        var sql = $"""SELECT * FROM "{_tableName}" WHERE "id" = @Id""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var dbResult = connection.QuerySingle<PostgresStorageModel>(sql, new { Id = (int)id });
        var dataResult = StorageHelper.Deserialize<T>(dbResult.Json);
        if (dataResult.IsError)
        {
            return dataResult.FirstError;
        }

        var data = dataResult.Value;
        if (dbResult.Id != data.Id)
        {
            return Errors.Fail($"Database id \"{dbResult.Id}\" does not match the json id \"{data.Id}\"");
        }

        return data;
    }

    public ErrorOr<T> Add(T data)
    {
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var nextId = connection.QuerySingle<int>($"SELECT nextval(pg_get_serial_sequence('{_tableName}', 'id'));");

        data.Id = (uint)nextId;
        data.UpdatedUtc = DateTime.UtcNow;
        data.CreatedUtc = DateTime.UtcNow;

        var json = StorageHelper.Serialize(data);
        if (json.IsError)
        {
            return json.FirstError;
        }

        var rowsAffected = connection.Execute(
            $"""INSERT INTO "{_tableName}" ("id", "json") VALUES (@Id, @Json)""",
            new { Id = nextId, Json = json.Value });
        if (rowsAffected != 1)
        {
            return Errors.Fail($"{rowsAffected} rows affected");
        }

        return data;
    }

    public ErrorOr<Success> Update(T data)
    {
        var getResult = Get(data.Id);
        if (getResult.IsError)
        {
            return getResult.FirstError;
        }
        
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        data.UpdatedUtc = DateTime.UtcNow;
        data.CreatedUtc = getResult.Value.CreatedUtc;
        
        var json = StorageHelper.Serialize(data);
        if (json.IsError)
        {
            return json.FirstError;
        }

        var affectedRows = connection.Execute(
            $"""UPDATE "{_tableName}" SET "json" = @Json where "id" = @Id""",
            new { id = (int)data.Id, Json = json.Value });
        if (affectedRows != 1)
        {
            return Errors.Fail($"{affectedRows} rows affected");
        }

        return Result.Success;
    }

    public ErrorOr<Deleted> Delete(uint id)
    {
        var sql = $"""DELETE FROM "{_tableName}" WHERE "id" = @id""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var affectedRows = connection.Execute(sql, new { id = (int)id });
        if (affectedRows != 1)
        {
            return Errors.Fail($"{affectedRows} rows affected");
        }

        return Result.Deleted;
    }

    public bool Exist(uint id)
    {
        var existsSql = $"""select count(1) from "{_tableName}" where "id" = @Id""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var exists = connection.ExecuteScalar<bool>(existsSql, new { Id = id });
        return exists;
    }
}