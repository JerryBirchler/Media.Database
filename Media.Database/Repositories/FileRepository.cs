using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Helpers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using System.Collections.Concurrent;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of IFileRepository. Writes go to PostgreSQL synchronously
/// within a transaction; Scylla and the word index are kept in sync separately and asynchronously
/// by the CDC pipeline (Media.Common.Cdc.CdcConsumerService dispatching to
/// Cdc.FilesCdcSyncHandler and Media.Worker's word-index handler), reading Postgres's own
/// write-ahead log rather than this repository fanning out to those stores itself. Reads are
/// split the same way: <see cref="GetCurrentPageIdentifiersBySourceMachineId"/> and
/// <see cref="GetByIds"/> let a caller (see FilesPaginationService in Media.Api) use PostgreSQL to
/// decide which rows and in what order, then hydrate full content from Scylla, falling back to
/// PostgreSQL per-row only when Scylla doesn't have it yet.
/// </summary>
public class FileRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    Func<IUnitOfWork> unitOfWorkFactory,
    ILogger<FileRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(scyllaProvider), IFileRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<FileRepository> _logger = logger.Initializer();

    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;

    public async Task<Files?> GetById(Guid id)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryFiles.GetByIdSql,
                p => p.AddWithValue(pn.Id, id),
                reader => reader.ToFile());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetById failed for FileId {Id}", id);
            throw;
        }
    }

    public async Task<Files?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryFiles.GetCurrentBySourceMachineIdSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
                },
                reader => reader.ToFile());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath!);
            throw;
        }
    }

    public async Task<List<Files>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryFiles.GetCurrentPagesBySourceMachineIdSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToFile());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentPagesBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath!);
            throw;
        }
    }

    public async Task<List<(Guid Id, string OriginalFilePath)>> GetCurrentPageIdentifiersBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryFiles.GetCurrentPageIdentifiersBySourceMachineIdSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToFileIdentifier());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetCurrentPageIdentifiersBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath!);
            throw;
        }
    }

    public async Task<List<Files>> GetByIds(IEnumerable<Guid> ids, int maxDegreeOfParallelism)
    {
        var results = new ConcurrentBag<Files>();

        await Parallel.ForEachAsync(
            ids,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            async (id, cancellationToken) =>
            {
                var file = await GetByIdPreferringScylla(id);
                if (file is not null)
                    results.Add(file);
            });

        return [.. results];
    }

    /// <summary>
    /// Looks up a single file by id, preferring Scylla and falling back to PostgreSQL when Scylla
    /// has no row yet (CDC lag) or is unreachable. A Scylla failure here is deliberately not
    /// rethrown -- PostgreSQL is the durable source of truth, so a temporarily unavailable Scylla
    /// cluster should degrade this request to a normal PostgreSQL read rather than fail it.
    /// </summary>
    private async Task<Files?> GetByIdPreferringScylla(Guid id)
    {
        var log = _logger.WithCaller();

        try
        {
            var fromScylla = await _cqlExecutor.QuerySingleAsync(
                QueryFiles.GetByIdCql,
                p => p.AddWithValue(pn.Id, id),
                row => row.ToFile());

            if (fromScylla is not null)
                return fromScylla;
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable fetching FileId {Id}; falling back to Postgres", id);
            await TryHealScyllaSessionAsync(_logger, nameof(GetByIdPreferringScylla));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Scylla lookup failed for FileId {Id}; falling back to Postgres", id);
        }

        return await GetById(id);
    }

    public async Task<List<Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5, int maxDegreeOfParallelism = 5)
    {
        try
        {
            var ids = await _sqlExecutor.QueryManyAsync(
                QueryFiles.GetHistoryIdsBySourceMachineIdSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath);
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToId());

            if (ids.Count == 0)
                return [];

            var hydrated = await GetByIds(ids, maxDegreeOfParallelism);
            var hydratedById = hydrated.ToDictionary(file => file.Id);

            return ids
                .Where(id => hydratedById.ContainsKey(id))
                .Select(id => hydratedById[id])
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHistoryPagesBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath);
            throw;
        }
    }

    public async Task<Files?> Upsert(int sourceMachineId, UploadFileRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var existingId = await _sqlExecutor.QuerySingleValueAsync(
                uow,
                QueryFiles.ExistsSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
                    p.AddWithValue(pn.LastFileUpdate, (object)request.LastFileUpdate! ?? DBNull.Value);
                },
                reader => reader.ToId());

            if (existingId.HasValue)
            {
                await uow.RollbackAsync();
                return new Files
                {
                    Id = existingId.Value,
                    Exists = true
                };
            }

            var file = await _sqlExecutor.QuerySingleAsync(
                uow,
                QueryFiles.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
                    p.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
                    p.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);
                },
                reader => reader.ToFile());

            if (file == null)
            {
                await uow.RollbackAsync();
                return null;
            }

            await uow.CommitAsync();

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upsert transaction failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, request.OriginalFilePath);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<UpdateFileResponse> Update(
        Guid id,
        UpdateFileRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var file = await _sqlExecutor.QuerySingleAsync(
                uow,
                QueryFiles.UpdateSql,
                p =>
                {
                    p.AddWithValue(pn.Id, id);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
                    p.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
                    p.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);
                },
                reader => reader.ToFile());

            if (file == null)
            {
                await uow.RollbackAsync();
                return new UpdateFileResponse { File = null };
            }

            await uow.CommitAsync();

            return new UpdateFileResponse { File = file };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update transaction failed for FileId {Id}", id);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<Files?> Delete(Guid id)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryFiles.DeleteSql,
                p => p.AddWithValue(pn.Id, id),
                reader => reader.ToFile());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for FileId {Id}", id);
            throw;
        }
    }

    public async Task<List<Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryFiles.DeleteHistorySql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath);
                },
                reader => reader.ToFile());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteHistoryBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath);
            throw;
        }
    }

    public async Task SetThumbnailGeneratedOn(Guid id, DateTimeOffset generatedOn)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(
                QueryFiles.SetThumbnailGeneratedOnSql,
                p =>
                {
                    p.AddWithValue(pn.Id, id);
                    p.AddWithValue(pn.ThumbnailGeneratedOn, generatedOn);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetThumbnailGeneratedOn failed for FileId {Id}", id);
            throw;
        }
    }

    public async Task RefreshView()
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(QueryFiles.RefreshViewSql, static _ => { });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RefreshView failed for View_Current_Files");
            throw;
        }
    }
}
