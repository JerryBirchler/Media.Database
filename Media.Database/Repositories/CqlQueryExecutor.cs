using Cassandra;
using Media.Common.Providers;
using Media.Database.Repositories.Queries.Helpers;

namespace Media.Database.Repositories;

/// <summary>
/// The Scylla/Cassandra counterpart to <see cref="SqlQueryExecutor"/>: the only class in this project
/// that binds and executes CQL directly against a live session. Everything else depends on
/// <see cref="ICqlQueryExecutor"/> so it can be unit tested without a live cluster.
/// </summary>
public class CqlQueryExecutor(IScyllaSessionProvider scyllaProvider) : ICqlQueryExecutor
{
    /// <summary>
    /// Executes a query that returns a single result asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing the result.</returns>
    public async Task<T?> QuerySingleAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map) where T : class
    {
        var (found, value) = await TryReadSingleAsync(cql, configureParameters, map);
        return found ? value : null;
    }

    /// <summary>
    /// Executes a query that returns a single value asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the value type.</param>
    /// <returns>A task representing the asynchronous operation, containing the value.</returns>
    public async Task<T?> QuerySingleValueAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map) where T : struct
    {
        var (found, value) = await TryReadSingleAsync(cql, configureParameters, map);
        return found ? value : null;
    }

    /// <summary>
    /// Executes a query that returns multiple results asynchronously.
    /// </summary>
    /// <typeparam name="T">The type of the results.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of results.</returns>
    public async Task<List<T>> QueryManyAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map)
    {
        var rowSet = await ExecuteRowSetAsync(cql, configureParameters);
        return [.. rowSet.Select(map)];
    }

    /// <summary>
    /// Executes a CQL command that does not return any rows asynchronously.
    /// </summary>
    /// <param name="cql">The CQL command to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the command parameters.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ExecuteAsync(string cql, Action<SortedDictionary<string, object>> configureParameters)
    {
        var command = GetSession().GetCqlCommand(cql);
        configureParameters(command.Parameters);
        await command.ExecuteRowSet();
    }

    /// <summary>
    /// Executes a query and maps at most one row. Returns a found/value pair rather than a bare
    /// <c>T?</c>: for an unconstrained T, <c>default</c> is the value type's zero value (e.g. 0), not
    /// "no value" — only a caller that knows T is a class or a struct can turn "no row" into the right
    /// null/Nullable&lt;T&gt; shape, so that decision is left to the properly-constrained public
    /// <see cref="QuerySingleAsync{T}"/>/<see cref="QuerySingleValueAsync{T}"/> overloads that call this.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <param name="map">A delegate to map a row to the result type.</param>
    /// <returns>A task representing the asynchronous operation, containing whether a row was found and, if so, its mapped value.</returns>
    private async Task<(bool Found, T Value)> TryReadSingleAsync<T>(string cql, Action<SortedDictionary<string, object>> configureParameters, Func<Row, T> map)
    {
        var rowSet = await ExecuteRowSetAsync(cql, configureParameters);
        var row = rowSet.FirstOrDefault();
        return row is null ? (false, default!) : (true, map(row));
    }

    /// <summary>
    /// Binds <paramref name="configureParameters"/> and executes <paramref name="cql"/>, returning the resulting row set.
    /// </summary>
    /// <param name="cql">The CQL query to execute, with named (<c>@name</c>) parameters.</param>
    /// <param name="configureParameters">A delegate to configure the query parameters.</param>
    /// <returns>A task representing the asynchronous operation, containing the row set.</returns>
    private async Task<RowSet> ExecuteRowSetAsync(string cql, Action<SortedDictionary<string, object>> configureParameters)
    {
        var command = GetSession().GetCqlCommand(cql);
        configureParameters(command.Parameters);
        return await command.ExecuteRowSet();
    }

    /// <summary>
    /// Gets the active Scylla/Cassandra session.
    /// </summary>
    /// <returns>The current <see cref="ISession"/>.</returns>
    private ISession GetSession() => scyllaProvider.GetSession();
}
