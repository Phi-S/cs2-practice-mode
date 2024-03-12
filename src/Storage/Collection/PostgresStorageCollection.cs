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


    public ErrorOr<List<T>> GetAll()
    {
        var sql = $"""SELECT * FROM "{_tableName}" """;
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
        var dbResult = connection.QuerySingle<PostgresStorageModel>(sql, new { Id = id });
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
        const string getNextIdSql = "SELECT nextval('Id_seq');";
        var nextId = connection.QuerySingle<uint>(getNextIdSql);

        data.Id = nextId;
        data.UpdatedUtc = DateTime.UtcNow;
        data.CreatedUtc = DateTime.UtcNow;

        var json = StorageHelper.Serialize(data);
        if (json.IsError)
        {
            return json.FirstError;
        }

        var sql = $"""INSERT INTO "{_tableName}" ("id","json") VALUES (@Id, @Json)""";
        var rowsAffected = connection.Execute(sql, new { Id = nextId, Json = json.Value });
        if (rowsAffected != 1)
        {
            return Errors.Fail($"{rowsAffected} rows affected");
        }

        return data;
    }

    public ErrorOr<Success> Update(T data)
    {
        var sql = $"""UPDATE "{_tableName}" SET "json" = @Json where "id" = @Id""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        data.UpdatedUtc = DateTime.UtcNow;
        var json = StorageHelper.Serialize(data);

        var rowsAffected = connection.Execute(sql, new { data.Id, Json = json });
        if (rowsAffected != 1)
        {
            return Errors.Fail($"{rowsAffected} rows affected");
        }

        return Result.Success;
    }

    public ErrorOr<Deleted> Delete(uint id)
    {
        throw new NotImplementedException();
    }

    public bool Exist(uint id)
    {
        var existsSql = $"""select count(1) from "{_tableName}" where "id" = @Id""";
        using IDbConnection connection = new NpgsqlConnection(_connectionString);
        var exists = connection.ExecuteScalar<bool>(existsSql, new { Id = id });
        return exists;
    }

    private void CreateTable()
    {
        var connection = new NpgsqlConnection(_connectionString);
        var command =
            new NpgsqlCommand($"""
                               CREATE TABLE "{_tableName}" (
                               "id" SERIAL NOT NULL,
                               "json" TEXT NOT NULL,
                               PRIMARY KEY ("id"))
                               """, connection);
        connection.Open();
        command.ExecuteNonQuery();
        connection.Close();
    }
}