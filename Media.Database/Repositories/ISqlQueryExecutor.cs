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
    /// <summary>Runs <paramref name="sql"/> on its own connection and maps the first row, or null if there were no rows.</summary>
    Task<T?> QuerySingleAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class;

    /// <summary>Runs <paramref name="sql"/> on its own connection and maps the first row to a value type, or null if there were no rows.</summary>
    Task<T?> QuerySingleValueAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct;

    /// <summary>Runs <paramref name="sql"/> on its own connection and maps every row.</summary>
    Task<List<T>> QueryManyAsync<T>(string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map);

    /// <summary>Runs <paramref name="sql"/> on its own connection for its side effect, returning the affected row count.</summary>
    Task<int> ExecuteAsync(string sql, Action<NpgsqlParameterCollection> configureParameters);

    /// <summary>Runs <paramref name="sql"/> on <paramref name="unitOfWork"/>'s connection/transaction and maps the first row, or null if there were no rows.</summary>
    Task<T?> QuerySingleAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : class;

    /// <summary>Runs <paramref name="sql"/> on <paramref name="unitOfWork"/>'s connection/transaction and maps the first row to a value type, or null if there were no rows.</summary>
    Task<T?> QuerySingleValueAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map) where T : struct;

    /// <summary>Runs <paramref name="sql"/> on <paramref name="unitOfWork"/>'s connection/transaction and maps every row.</summary>
    Task<List<T>> QueryManyAsync<T>(IUnitOfWork unitOfWork, string sql, Action<NpgsqlParameterCollection> configureParameters, Func<NpgsqlDataReader, T> map);
}
