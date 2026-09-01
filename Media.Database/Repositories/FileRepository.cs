using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Helpers;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using Serilog.Core;


#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IFileRepository"/>. Writes go to PostgreSQL
/// synchronously within a transaction; the equivalent Scylla/Cassandra rows are then updated
/// on a background task queue, with self-healing if the Scylla session becomes unreachable.
/// </summary>
/// <param name="sqlExecutor">The SQL query executor.</param>
/// <param name="scyllaProvider">The Scylla session provider.</param>
/// <param name="unitOfWorkFactory">The factory function to create a unit of work.</param>
/// <param name="changeWordMapper">The mapper for change word requests.</param>
/// <param name="backgroundTaskQueue">The background task queue.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="levelSwitch">The logging level switch.</param>
public class FileRepository(
    ISqlQueryExecutor sqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    Func<IUnitOfWork> unitOfWorkFactory,
    IMapChangeWordRequests changeWordMapper,
    IBackgroundTaskQueue backgroundTaskQueue,
    ILogger<FileRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(scyllaProvider), IFileRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapChangeWordRequests _changeWordMapper = changeWordMapper;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue = backgroundTaskQueue;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<FileRepository> _logger = logger.Initializer();

    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;
    private readonly int _scyllaMaxBatchsize = scyllaProvider.MaxBatchSize;

    /// <summary>
    /// Gets a file by its unique identifier asynchronously.            
    /// </summary>
    /// <param name="id">The unique identifier of the file.</param>
    /// <returns>A task representing the asynchronous operation, containing the file.</returns>
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

    /// <summary>
    /// Gets the current pages of files by source machine ID asynchronously.
    /// </summary>
    /// <param name="sourceMachineId">The ID of the source machine.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <param name="limit">The maximum number of files to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, containing the file.</returns>
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

    /// <summary>
    /// Gets the current pages of files by source machine ID asynchronously.
    /// </summary>
    /// <param name="sourceMachineId">The ID of the source machine.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <param name="limit">The maximum number of files to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of files.</returns>
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

    /// <summary>
    /// Gets the history pages of files by source machine ID asynchronously.
    /// </summary>
    /// <param name="sourceMachineId">The ID of the source machine.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <param name="limit">The maximum number of files to retrieve.</param>
    /// <returns>A task representing the asynchronous operation, containing a list of files.</returns>
    public async Task<List<Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryFiles.GetHistoryPagesBySourceMachineIdSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath);
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToFile());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetHistoryPagesBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath);
            throw;
        }
    }

    /// <summary>
    /// Inserts or updates a file in the database asynchronously.
    /// </summary>
    /// <param name="request">The upload file request containing the file details.</param>
    /// <returns>A task representing the asynchronous operation, containing the inserted or updated file.</returns>
    public async Task<Files?> Upsert(UploadFileRequest request)
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
                    p.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
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

            var previousIds = await _sqlExecutor.QueryManyAsync(
                uow,
                QueryFiles.GetPreviousIdsSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
                },
                reader => reader.ToId());

            var file = await _sqlExecutor.QuerySingleAsync(
                uow,
                QueryFiles.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
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

            _ = QueueBackgroundUpdateAsync(file, previousIds);
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upsert transaction failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                request.SourceMachineId, request.OriginalFilePath);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    /// <summary>
    /// Updates a file in the database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to update.</param>
    /// <param name="request">The update request containing the new metadata.</param>
    /// <returns>A task representing the asynchronous operation, containing the update response.</returns>
    public async Task<UpdateFileResponse> Update(
        Guid id,
        UpdateFileRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var currentFile = await _sqlExecutor.QuerySingleAsync(
                uow,
                QueryFiles.GetByIdSql,
                p => p.AddWithValue(pn.Id, id),
                reader => reader.ToFile());

            if (currentFile == null)
            {
                await uow.RollbackAsync();
                return new UpdateFileResponse { File = null };
            }

            UpdateFileResponse response = new()
            {
                Updates = GetUpdates(currentFile, request)
            };

            response.File = await _sqlExecutor.QuerySingleAsync(
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

            if (response.File == null)
            {
                await uow.RollbackAsync();
                return response;
            }

            await uow.CommitAsync();

            _ = QueueBackgroundUpdateMetadataAsync(response.File);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update transaction failed for FileId {Id}", id);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    /// <summary>
    /// Gets the list of updates for a file based on the current metadata and the update request.
    /// </summary>
    /// <param name="current">The current file.</param>
    /// <param name="request">The update request.</param>
    /// <returns>A list of change word requests representing the updates.</returns>
    private List<ChangeWordRequest> GetUpdates(Files current, UpdateFileRequest request)
    {
        var updates = new List<ChangeWordRequest>();
        var curMeta = current.Metadata;
        var newMeta = request.Metadata;

        if (curMeta is null && newMeta is null)
            return updates;

        updates.ProcessList(curMeta?.Names, newMeta?.Names, current, WordOrigin.Name, _changeWordMapper);
        updates.ProcessList(curMeta?.KeyWords, newMeta?.KeyWords, current, WordOrigin.Keyword, _changeWordMapper);

        updates.ProcessScalar(curMeta?.Title, newMeta?.Title, current, WordOrigin.FromTitle, _changeWordMapper);
        updates.ProcessScalar(curMeta?.Description, newMeta?.Description, current, WordOrigin.FromDescription, _changeWordMapper);
        updates.ProcessScalar(curMeta?.Event, newMeta?.Event, current, WordOrigin.FromEvent, _changeWordMapper);
        updates.ProcessScalar(curMeta?.Location, newMeta?.Location, current, WordOrigin.FromLocation, _changeWordMapper);

        return updates;
    }

    /// <summary>
    /// Queues a background task to update a file in the Scylla database asynchronously.
    /// </summary>
    /// <param name="file">The file to update.</param>
    /// <param name="previousIds">The list of previous IDs associated with the file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task QueueBackgroundUpdateAsync(Files file, List<Guid> previousIds)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await UpdateCqlAsync(file, previousIds);
        });
    }

    /// <summary>
    /// Updates a file in the Scylla database asynchronously.
    /// </summary>
    /// <param name="file">The file to update.</param>
    /// <param name="previousIds">The list of previous IDs associated with the file.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdateCqlAsync(Files file, List<Guid> previousIds)
    {
        var log = _logger.WithCaller();

        try
        {
            var cqlConnection = GetCqlConnection();

            if (previousIds.Count > 0)
            {
                CqlCommand cqlCommand = cqlConnection.GetCqlCommand(
                QueryFiles.InactivateCql, _scyllaMaxBatchsize)!;

                cqlCommand.BeginBatch();

                var tasks = previousIds.Select(previousId => cqlCommand.AddQuery(previousId));

                await Task.WhenAll(tasks);
                await cqlCommand.EndBatch();
            }

            var cqlCommand2 = cqlConnection.GetCqlCommand(QueryFiles.UpsertCql);
            cqlCommand2.Parameters.AddWithValue(pn.Id, file.Id);
            cqlCommand2.Parameters.AddWithValue(pn.SourceMachineId, file.SourceMachineId);
            cqlCommand2.Parameters.AddWithValue(pn.OriginalFilePath, file.OriginalFilePath);
            cqlCommand2.Parameters.AddWithValue(pn.InsertedOn, file.InsertedOn);
            cqlCommand2.Parameters.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
            cqlCommand2.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            cqlCommand2.Parameters.AddWithValue(pn.IsCurrent, file.IsCurrent);
            cqlCommand2.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await cqlCommand2.ExecuteRowSet();

            log.LogInformation("Background NoSQL upsert completed for FileId {Id}", file.Id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", file.Id);
            await TryHealScyllaSessionAsync(nameof(UpdateCqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundUpdate task failed for FileId {Id}", file.Id);
            throw;
        }
    }

    /// <summary>
    /// Queues a background task to update the metadata of a file in the Scylla database asynchronously.
    /// </summary>
    /// <param name="file">The file whose metadata is to be updated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task QueueBackgroundUpdateMetadataAsync(Files file)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await UpdateMetadataCqlAsync(file);
        });
    }

    /// <summary>
    /// Updates the metadata of a file in the Scylla database asynchronously.       
    /// </summary>
    /// <param name="file"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdateMetadataCqlAsync(Files file)
    {
        var log = _logger.WithCaller();

        try
        {
            var cqlConnection = GetCqlConnection();
            var cqlCommand = cqlConnection.GetCqlCommand(QueryFiles.UpdateCql);

            cqlCommand.Parameters.AddWithValue(pn.Id, file.Id);
            cqlCommand.Parameters.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
            cqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            cqlCommand.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await cqlCommand.ExecuteRowSet();

            log.LogInformation("Background NoSQL metadata update completed for FileId {Id}", file.Id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", file.Id);
            await TryHealScyllaSessionAsync(nameof(UpdateMetadataCqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundUpdate task failed for FileId {Id}", file.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from the SQL database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation, containing the deleted file if found.</returns>
    public async Task<Files?> Delete(Guid id)
    {
        try
        {
            var file = await _sqlExecutor.QuerySingleAsync(
                QueryFiles.DeleteSql,
                p => p.AddWithValue(pn.Id, id),
                reader => reader.ToFile());

            if (file != null)
                _ = QueueBackgroundDeleteAsync(id);

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for FileId {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes the history of files by source machine ID and original file path from the Scylla database asynchronously.
    /// </summary>
    /// <param name="sourceMachineId">The ID of the source machine.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of deleted files.</returns>
    public async Task<List<Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath)
    {
        try
        {
            var files = await _sqlExecutor.QueryManyAsync(
                QueryFiles.DeleteHistorySql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath);
                },
                reader => reader.ToFile());

            if (files.Count > 0)
                _ = QueueBackgroundDeleteAsync(files);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteHistoryBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath);
            throw;
        }
    }

    /// <summary>
    /// Queues a background task to delete a file from the Scylla database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task QueueBackgroundDeleteAsync(Guid id)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await DeleteCqlAsync(id);
        });
    }

    /// <summary>
    /// Deletes a file from the Scylla database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteCqlAsync(Guid id)
    {
        var log = _logger.WithCaller();

        try
        {
            var cqlConnection = GetCqlConnection();
            var cqlCommand = cqlConnection.GetCqlCommand(QueryFiles.DeleteCql);

            cqlCommand.Parameters.AddWithValue(pn.Id, id);
            await cqlCommand.ExecuteRowSet();

            log.LogInformation("Background NoSQL delete completed for FileId {Id}", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", id);
            await TryHealScyllaSessionAsync(nameof(DeleteCqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundDelete task failed for FileId {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Queues a background task to delete a batch of files from the Scylla database asynchronously.
    /// </summary>
    /// <param name="files">The list of files to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>

    private async Task QueueBackgroundDeleteAsync(List<Files> files)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await DeleteCqlBatchAsync(files);
        });
    }

    /// <summary>
    /// Deletes a batch of files from the Scylla database asynchronously.
    /// </summary>
    /// <param name="files">The list of files to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteCqlBatchAsync(List<Files> files)
    {
        var log = _logger.WithCaller();

        try
        {
            var cqlConnection = GetCqlConnection();

            CqlCommand cqlCommand = cqlConnection.GetCqlCommand(
                QueryFiles.DeleteCql, _scyllaMaxBatchsize)!;

            cqlCommand.BeginBatch();

            var tasks = files.Select(file => cqlCommand.AddQuery(file.Id));

            await Task.WhenAll(tasks);
            await cqlCommand.EndBatch();

            log.LogInformation("Background NoSQL batch delete completed for {Count} files", files.Count);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable during batch delete");
            await TryHealScyllaSessionAsync(nameof(DeleteCqlBatchAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundDelete batch task failed");
            throw;
        }
    }

    /// <summary>
    /// Cassandra driver exceptions that indicate the cluster/session is unreachable, as opposed to a single
    /// query timing out against an otherwise healthy cluster. Only these warrant rebuilding the session.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a connectivity issue with the Scylla cluster; otherwise, false.</returns>
    private static bool IsScyllaConnectivityException(Exception ex) =>
        ex is NoHostAvailableException or UnavailableException or OperationTimedOutException;

    /// <summary>
    /// Attempts to heal the Scylla session without letting a healing failure (e.g. a busy self-heal lock)
    /// mask the original exception that triggered the heal attempt.
    /// </summary>
    /// <param name="methodName">The name of the method that triggered the heal attempt.</param>    
    private async Task TryHealScyllaSessionAsync(string methodName)
    {
        try
        {
            await ScyllaProvider!.HealSessionAsync(ScyllaProvider.GetCurrentSessionId(), methodName);
        }
        catch (Exception healEx)
        {
            _logger.WithCaller().LogError(healEx, "Scylla session heal attempt failed in {OriginatingMethod}", methodName);
        }
    }
}
