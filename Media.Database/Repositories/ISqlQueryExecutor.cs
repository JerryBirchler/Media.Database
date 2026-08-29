using Media.Common.Transactions;
using Npgsql;

namespace Media.Database.Repositories;

/// <summary>
/// Executes hand-written SQL against PostgreSQL and maps rows to domain objects, owning the
/// connection lifecycle so callers never touch <see cref="NpgsqlConnection"/> or
/// <see cref="NpgsqlDataReader"/> directly. This is the seam that makes repository methods
/// mockable: implementations are the only code that opens a real connection.
/// </summary>
public interface ISqlQueryExecutor
{
    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<T?> QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class;

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    Task<T?> QuerySingleValueAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct;

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of results.</returns>
    Task<List<T>> QueryManyAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map);

    /// <summary>
    /// Executes a command that does not return any results asynchronously.
    /// </summary>
    /// <param name="sql">The SQL command to execute.</param>
    /// <param name="configureParameters">A delegate to configure the command parameters.</param>
    /// <returns>A task representing the asynchronous operation, containing the number of affected rows.</returns>
    Task<int> ExecuteAsync(string sql, Action<NpgsqlParameterCollection> configureParameters);

    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the connection and transaction.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<T?> QuerySingleAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class;

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the connection and transaction.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    Task<T?> QuerySingleValueAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct;

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="unitOfWork">The unit of work containing the connection and transaction.</param>
    /// <param name="sql">The SQL query to execute.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map the data reader to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of results.</returns>
    Task<List<T>> QueryManyAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map);
}
