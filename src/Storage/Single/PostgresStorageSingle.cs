using System.Data;
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

    public ErrorOr<T> Get()
    {
        var sql = $"""SELECT TOP 1 FROM "{_tableName}""";
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
        throw new NotImplementedException();
    }

    public ErrorOr<Deleted> Delete()
    {
        throw new NotImplementedException();
    }

    public bool Exists()
    {
        throw new NotImplementedException();
    }

    private void CreateTable()
    {
        var connection = new NpgsqlConnection(_connectionString);
        var command =
            new NpgsqlCommand($"""
                               CREATE TABLE "{_tableName}" ("json" TEXT NOT NULL)
                               """, connection);
        connection.Open();
        command.ExecuteNonQuery();
        connection.Close();

        var oneRowCommand = new NpgsqlCommand("""
                                              CREATE UNIQUE INDEX
                                               one_row_only_uidx ON test_data (( TRUE ));
                                              """);

        connection.Open();
        oneRowCommand.ExecuteNonQuery();
        connection.Close();
    }
}