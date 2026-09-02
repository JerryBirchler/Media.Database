using Cassandra;

namespace Media.Database.Repositories;

/// <summary>
/// Executes hand-written CQL against Scylla/Cassandra and maps rows to domain objects, owning the
/// session/statement lifecycle so callers never touch <see cref="ISession"/>, <see cref="Queries.Helpers.CqlCommand"/>,
/// or <see cref="Row"/> directly. This is the seam that makes repository methods mockable for Scylla,
/// mirroring <see cref="ISqlQueryExecutor"/> for PostgreSQL.
/// </summary>
/// <remarks>
/// There is no unit-of-work overload here: unlike PostgreSQL writes, Scylla/Cassandra writes in this
/// codebase are not participants in a relational transaction (see <see cref="BaseRepository"/>), so
/// there is nothing analogous to <see cref="Media.Common.Transactions.IUnitOfWork"/> to thread through.
/// </remarks>
public interface ICqlQueryExecutor
{
    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    Task<T?> QuerySingleAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map) where T : class;

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    Task<T?> QuerySingleValueAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map) where T : struct;

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of results.</returns>
    Task<List<T>> QueryManyAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map);

    /// <summary>
    /// Executes a command that does not return any rows asynchronously.
    /// </summary>
    /// <param name="cql">The CQL command to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the command parameters.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ExecuteAsync(string cql, Action<SortedDictionary<string, object>> configureParameters);
}
