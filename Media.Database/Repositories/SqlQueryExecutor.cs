using Media.Common.Providers;
using Media.Common.Transactions;
using Npgsql;

namespace Media.Database.Repositories;

/// <summary>
/// The only class in this project that opens a real PostgreSQL connection. Everything else
/// depends on <see cref="ISqlQueryExecutor"/> so it can be unit tested without a live database.
/// </summary>
/// <remarks>
/// A read whose pooled connection was dropped is tried again on a fresh one (<see cref="ReadRetry"/>);
/// the pool is cleared first, since the connections beside the dead one were likely dropped with it.
/// Writes, and anything inside a unit of work, are never repeated.
/// </remarks>
/// <param name="postgresProvider">Gives the connection string.</param>
/// <param name="retry">When to try a read again; <see cref="ReadRetry.Default"/> when not given.</param>
public class SqlQueryExecutor(IPostgresConnectionProvider postgresProvider, ReadRetry? retry = null) : ISqlQueryExecutor
{
    private readonly ReadRetry _retry = retry ?? ReadRetry.Default;

    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public Task<T?> QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class =>
        _retry.RunAsync(sql, async () =>
        {
            await using var connection = await OpenConnectionAsync();
            var (found, value) = await TryReadSingleAsync(connection, sql, configureParameters, map);
            return found ? value : null;
        }, IsConnectionFailure, ClearPool);

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    public Task<T?> QuerySingleValueAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct =>
        _retry.RunAsync<T?>(sql, async () =>
        {
            await using var connection = await OpenConnectionAsync();
            var (found, value) = await TryReadSingleAsync(connection, sql, configureParameters, map);
            return found ? value : null;
        }, IsConnectionFailure, ClearPool);

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results.</returns>
    public Task<List<T>> QueryManyAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) =>
        _retry.RunAsync(sql, async () =>
        {
            await using var connection = await OpenConnectionAsync();
            return await QueryManyAsync(connection, sql, configureParameters, map);
        }, IsConnectionFailure, ClearPool);

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
    public async Task<int> ExecuteAsync(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters)
    {
        await using var command = new NpgsqlCommand(sql, unitOfWork.Connection, unitOfWork.CurrentTransaction);
        configureParameters(command.Parameters);
        return await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> QuerySingleAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class
    {
        var (found, value) = await TryReadSingleAsync(unitOfWork.Connection, sql, configureParameters, map);
        return found ? value : null;
    }

    /// <summary>
    /// Executes a query that returns a single value asynchronously within a unit of work.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    public async Task<T?> QuerySingleValueAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct
    {
        var (found, value) = await TryReadSingleAsync(unitOfWork.Connection, sql, configureParameters, map);
        return found ? value : null;
    }

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
    /// A failure of the connection itself -- dropped, reset, unreachable -- which Npgsql marks
    /// transient, as opposed to an error PostgreSQL returned for the statement.
    /// </summary>
    private static bool IsConnectionFailure(Exception exception) =>
        exception is NpgsqlException { IsTransient: true } and not PostgresException;

    /// <summary>
    /// Empties this database's connection pool, so the next attempt opens a fresh connection rather
    /// than taking another that was dropped along with the one that failed.
    /// </summary>
    private void ClearPool()
    {
        using var connection = new NpgsqlConnection(postgresProvider.GetConnectionString());
        NpgsqlConnection.ClearPool(connection);
    }

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
    /// Executes a query and maps at most one row. Returns a found/value pair rather than a bare
    /// <c>T?</c>: for an unconstrained T, <c>default</c> is the value type's zero value (e.g. 0), not
    /// "no value" — only a caller that knows T is a class or a struct can turn "no row" into the right
    /// null/Nullable&lt;T&gt; shape, so that decision is left to the properly-constrained public
    /// <c>QuerySingleAsync</c>/<c>QuerySingleValueAsync</c> overloads that call this.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="connection">The database connection.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing whether a row was found and, if so, its mapped value.</returns>
    private static async Task<(bool Found, T Value)> TryReadSingleAsync<T>(NpgsqlConnection connection, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map)
    {
        await using var command = new NpgsqlCommand(sql, connection);
        configureParameters(command.Parameters);
        await using var reader = await command.ExecuteReaderAsync();
        var found = await reader.ReadAsync();
        return (found, found ? map(reader) : default!);
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
