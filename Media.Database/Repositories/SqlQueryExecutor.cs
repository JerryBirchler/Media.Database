using Media.Common.Providers;
using Media.Common.Transactions;
using Npgsql;

namespace Media.Database.Repositories;

/// <summary>
/// The only class in this project that opens a real PostgreSQL connection. Everything else
/// depends on <see cref="ISqlQueryExecutor"/> so it can be unit tested without a live database.
/// </summary>
public class SqlQueryExecutor(IPostgresConnectionProvider postgresProvider) : ISqlQueryExecutor
{
    /// <inheritdoc/>
    public async Task<T?> QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class
    {
        await using var connection = await OpenConnectionAsync();
        return await QuerySingleAsync(connection, sql, configureParameters, map);
    }

    /// <inheritdoc/>
    public async Task<T?> QuerySingleValueAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct
    {
        await using var connection = await OpenConnectionAsync();
        return await QuerySingleValueAsync(connection, sql, configureParameters, map);
    }

    /// <inheritdoc/>
    public async Task<List<T>> QueryManyAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map)
    {
        await using var connection = await OpenConnectionAsync();
        return await QueryManyAsync(connection, sql, configureParameters, map);
    }

    /// <inheritdoc/>
    public async Task<int> ExecuteAsync(string sql, Action<NpgsqlParameterCollection> configureParameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        return await command.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public Task<T?> QuerySingleAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class =>
        QuerySingleAsync(unitOfWork.Connection, sql, configureParameters, map);

    /// <inheritdoc/>
    public Task<T?> QuerySingleValueAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct =>
        QuerySingleValueAsync(unitOfWork.Connection, sql, configureParameters, map);

    /// <inheritdoc/>
    public Task<List<T>> QueryManyAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) =>
        QueryManyAsync(unitOfWork.Connection, sql, configureParameters, map);

    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(postgresProvider.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    private static async Task<T?> QuerySingleAsync<T>(NpgsqlConnection connection, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : null;
    }

    private static async Task<T?> QuerySingleValueAsync<T>(NpgsqlConnection connection, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : null;
    }

    private static async Task<List<T>> QueryManyAsync<T>(NpgsqlConnection connection, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync();

        var results = new List<T>();
        while (await reader.ReadAsync())
            results.Add(map(reader));

        return results;
    }
}
