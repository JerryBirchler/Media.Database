using Npgsql;

namespace Media.Database.Transactions;

public interface IUnitOfWork : IDisposable, IAsyncDisposable
{
    Task<NpgsqlTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
    NpgsqlConnection Connection { get; }
    NpgsqlTransaction? CurrentTransaction { get; }
}
