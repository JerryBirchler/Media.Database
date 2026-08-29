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
    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public async Task<T?> QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class
    {
        await using var connection = await OpenConnectionAsync();
        return await QuerySingleAsync(connection, sql, configureParameters, map);
    }

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    public async Task<T?> QuerySingleValueAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct
    {
        await using var connection = await OpenConnectionAsync();
        return await QuerySingleValueAsync(connection, sql, configureParameters, map);
    }

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results.</returns>
    public async Task<List<T>> QueryManyAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map)
    {
        await using var connection = await OpenConnectionAsync();
        return await QueryManyAsync(connection, sql, configureParameters, map);
    }

    /// <summary>
    /// Executes a non-query SQL command asynchronously.
    /// </summary>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="configureParameters">A delegate to configure the command parameters.</param>
    /// <returns>A task representing the asynchronous operation, containing the number of rows affected.</returns>
    public async Task<int> ExecuteAsync(string sql, Action<NpgsqlParameterCollection> configureParameters)
    {
        await using var connection = await OpenConnectionAsync();
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        return await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Executes a query that returns a single result asynchronously within a unit of work.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public Task<T?> QuerySingleAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class =>
        QuerySingleAsync(unitOfWork.Connection, sql, configureParameters, map);

    /// <summary>
    /// Executes a query that returns a single value asynchronously within a unit of work.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    public Task<T?> QuerySingleValueAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct =>
        QuerySingleValueAsync(unitOfWork.Connection, sql, configureParameters, map);

    /// <summary>
    /// Executes a query that returns multiple results asynchronously within a unit of work.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results.</returns>
    public Task<List<T>> QueryManyAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) =>
        QueryManyAsync(unitOfWork.Connection, sql, configureParameters, map);

    /// <summary>
    /// Opens a new database connection asynchronously.
    /// </summary>
    /// <returns>A task representing the asynchronous operation, containing the open database connection.</returns>
    private async Task<NpgsqlConnection> OpenConnectionAsync()
    {
        var connection = new NpgsqlConnection(postgresProvider.GetConnectionString());
        await connection.OpenAsync();
        return connection;
    }

    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    private static async Task<T?> QuerySingleAsync<T>(NpgsqlConnection connection, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : null;
    }

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    private static async Task<T?> QuerySingleValueAsync<T>(NpgsqlConnection connection, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync();
        return await reader.ReadAsync() ? map(reader) : null;
    }

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>    
    /// <param name="connection">The database connection.</param>   
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results.</returns>
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
