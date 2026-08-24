using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Helpers;
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

public class FileRepository(
    IPostgresConnectionProvider postgresProvider,
    IScyllaSessionProvider scyllaProvider,
    Func<IUnitOfWork> unitOfWorkFactory,
    IMapChangeWordRequests changeWordMapper,
    IBackgroundTaskQueue backgroundTaskQueue,
    ILogger<FileRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(postgresProvider, scyllaProvider), IFileRepository
{
    private readonly IMapChangeWordRequests _changeWordMapper = changeWordMapper;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue = backgroundTaskQueue;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly ILogger<FileRepository> _logger = (new Func<ILogger<FileRepository>>(() =>
    {
        var className = ClassHelper.GetName();
        logger.LogInformation(true, ClassHelper.Initializing, args: className);
        return logger;
    })());

    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;
    private readonly int _scyllaMaxBatchsize = scyllaProvider.MaxBatchSize;

    public async Task<Files?> GetById(Guid id)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetByIdSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return reader.ToFile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "GetById failed for FileId {Id}", args: id);
            throw;
        }
    }

    public async Task<Files?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetCurrentBySourceMachineIdSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return reader.ToFile();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "GetCurrentBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                args: [sourceMachineId, originalFilePath!]);
            throw;
        }
    }

    public async Task<List<Files>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetCurrentPagesBySourceMachineIdSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
            sqlCommand.Parameters.AddWithValue(pn.Limit, limit);
            await using var reader = await sqlCommand.ExecuteReaderAsync();
            return await reader.ToFiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "GetCurrentPagesBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                args: [sourceMachineId, originalFilePath!]);
            throw;
        }
    }

    public async Task<List<Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetHistoryPagesBySourceMachineIdSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath);
            sqlCommand.Parameters.AddWithValue(pn.Limit, limit);
            await using var reader = await sqlCommand.ExecuteReaderAsync();
            return await reader.ToFiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "GetHistoryPagesBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                args: [sourceMachineId, originalFilePath]);
            throw;
        }
    }

    public async Task<Files?> Upsert(UploadFileRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            await using var sqlCommand = await uow.Connection.GetCommand(QueryFiles.ExistsSql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
            sqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, (object)request.LastFileUpdate! ?? DBNull.Value);

            Guid? existingId = null;
            await using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                if (await reader.ReadAsync())
                {
                    existingId = reader.ToId();
                }
            }

            if (existingId.HasValue)
            {
                await uow.RollbackAsync();
                return new Files
                {
                    Id = existingId.Value,
                    Exists = true
                };
            }

            await sqlCommand.DisposeAsync();

#pragma warning disable S1854
            List<Guid> previousIds = [];
#pragma warning restore S1854
            await using var sqlCommand2 = await uow.Connection.GetCommand(QueryFiles.GetPreviousIdsSql);
            sqlCommand2.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
            sqlCommand2.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);

            await using (var reader2 = await sqlCommand2.ExecuteReaderAsync())
                previousIds = await reader2.ToIds();

            await sqlCommand2.DisposeAsync();

            await using var sqlCommand3 = await uow.Connection.GetCommand(QueryFiles.UpsertSql);
            sqlCommand3.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
            sqlCommand3.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
            sqlCommand3.Parameters.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
            sqlCommand3.Parameters.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
            sqlCommand3.Parameters.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);

            Files? file = null;
            await using (var reader3 = await sqlCommand3.ExecuteReaderAsync())
            {
                if (await reader3.ReadAsync())
                {
                    file = reader3.ToFile();
                }
            }

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
            _logger.LogError(ex, true, "Upsert transaction failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                args: [request.SourceMachineId, request.OriginalFilePath]);

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

            await using var sqlCommand = await uow.Connection.GetCommand(QueryFiles.GetByIdSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);

            Files currentFile = null!;

            await using (var reader = await sqlCommand.ExecuteReaderAsync())
            {
                if (!await reader.ReadAsync())
                {
                    await uow.RollbackAsync();
                    return new UpdateFileResponse { File = null };
                }

                currentFile = reader.ToFile();
            }

            UpdateFileResponse response = new()
            {
                Updates = GetUpdates(currentFile, request)
            };

            await using var sqlCommand2 = await uow.Connection.GetCommand(QueryFiles.UpdateSql);
            sqlCommand2.Parameters.AddWithValue(pn.Id, id);
            sqlCommand2.Parameters.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
            sqlCommand2.Parameters.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
            sqlCommand2.Parameters.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);

            await using (var reader2 = await sqlCommand2.ExecuteReaderAsync())
            {
                if (!await reader2.ReadAsync())
                {
                    await uow.RollbackAsync();
                    return response;
                }

                response.File = reader2.ToFile();
            }

            await uow.CommitAsync();

            _ = QueueBackgroundUpdateMetadataAsync(response.File);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "Update transaction failed for FileId {Id}", args: id);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

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

    private async Task QueueBackgroundUpdateAsync(Files file, List<Guid> previousIds)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await UpdateNoSqlAsync(file, previousIds);
        });
    }

    private async Task UpdateNoSqlAsync(Files file, List<Guid> previousIds)
    {
        try
        {
            var noSqlConnection = GetNoSqlConnection();

            if (previousIds.Count > 0)
            {
                NoSqlCommand noSqlCommand = noSqlConnection.GetNoSqlCommand(
                QueryFiles.InactivateNoSql, _scyllaMaxBatchsize)!;

                noSqlCommand.BeginBatch();

                var tasks = previousIds.Select(previousId => noSqlCommand.AddQuery(previousId));

                await Task.WhenAll(tasks);
                await noSqlCommand.EndBatch();
            }

            var noSqlCommand2 = noSqlConnection.GetNoSqlCommand(QueryFiles.UpsertNoSql);
            noSqlCommand2.Parameters.AddWithValue(pn.Id, file.Id);
            noSqlCommand2.Parameters.AddWithValue(pn.SourceMachineId, file.SourceMachineId);
            noSqlCommand2.Parameters.AddWithValue(pn.OriginalFilePath, file.OriginalFilePath);
            noSqlCommand2.Parameters.AddWithValue(pn.InsertedOn, file.InsertedOn);
            noSqlCommand2.Parameters.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
            noSqlCommand2.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            noSqlCommand2.Parameters.AddWithValue(pn.IsCurrent, file.IsCurrent);
            noSqlCommand2.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await noSqlCommand2.ExecuteRowSet();

            _logger.LogInformation(true, "Background NoSQL upsert completed for FileId {Id}", args: file.Id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            _logger.LogError(ex, true, "Scylla cluster unavailable for FileId {Id}", args: file.Id);
            await TryHealScyllaSessionAsync(nameof(UpdateNoSqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "BackgroundUpdate task failed for FileId {Id}", args: file.Id);
            throw;
        }
    }

    private async Task QueueBackgroundUpdateMetadataAsync(Files file)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await UpdateMetadataNoSqlAsync(file);
        });
    }

    private async Task UpdateMetadataNoSqlAsync(Files file)
    {
        try
        {
            var noSqlConnection = GetNoSqlConnection();
            var noSqlCommand = noSqlConnection.GetNoSqlCommand(QueryFiles.UpdateNoSql);

            noSqlCommand.Parameters.AddWithValue(pn.Id, file.Id);
            noSqlCommand.Parameters.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
            noSqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            noSqlCommand.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await noSqlCommand.ExecuteRowSet();

            _logger.LogInformation(true, "Background NoSQL metadata update completed for FileId {Id}", args: file.Id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            _logger.LogError(ex, true, "Scylla cluster unavailable for FileId {Id}", args: file.Id);
            await TryHealScyllaSessionAsync(nameof(UpdateMetadataNoSqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "BackgroundUpdate task failed for FileId {Id}", args: file.Id);
            throw;
        }
    }


    public async Task<Files?> Delete(Guid id)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            var file = reader.ToFile();

            _ = QueueBackgroundDeleteAsync(id);
            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "Delete failed for FileId {Id}", args: id);
            throw;
        }
    }

    public async Task<List<Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteHistorySql);
            sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
            sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            var files = await reader.ToFiles();

            if (files.Count > 0)
                _ = QueueBackgroundDeleteAsync(files);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "DeleteHistoryBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                args: [sourceMachineId, originalFilePath]);
            throw;
        }
    }

    private async Task QueueBackgroundDeleteAsync(Guid id)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await DeleteNoSqlAsync(id);
        });
    }

    private async Task DeleteNoSqlAsync(Guid id)
    {
        try
        {
            var noSqlConnection = GetNoSqlConnection();
            var noSqlCommand = noSqlConnection.GetNoSqlCommand(QueryFiles.DeleteNoSql);

            noSqlCommand.Parameters.AddWithValue(pn.Id, id);
            await noSqlCommand.ExecuteRowSet();

            _logger.LogInformation(true, "Background NoSQL delete completed for FileId {Id}", args: id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            _logger.LogError(ex, true, "Scylla cluster unavailable for FileId {Id}", args: id);
            await TryHealScyllaSessionAsync(nameof(DeleteNoSqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "BackgroundDelete task failed for FileId {Id}", args: id);
            throw;
        }
    }

    private async Task QueueBackgroundDeleteAsync(List<Files> files)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await DeleteNoSqlBatchAsync(files);
        });
    }

    private async Task DeleteNoSqlBatchAsync(List<Files> files)
    {
        try
        {
            var noSqlConnection = GetNoSqlConnection();

            NoSqlCommand noSqlCommand = noSqlConnection.GetNoSqlCommand(
                QueryFiles.DeleteNoSql, _scyllaMaxBatchsize)!;

            noSqlCommand.BeginBatch();

            var tasks = files.Select(file => noSqlCommand.AddQuery(file.Id));

            await Task.WhenAll(tasks);
            await noSqlCommand.EndBatch();

            _logger.LogInformation(true, "Background NoSQL batch delete completed for {Count} files", args: files.Count);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            _logger.LogError(ex, true, "Scylla cluster unavailable during batch delete");
            await TryHealScyllaSessionAsync(nameof(DeleteNoSqlBatchAsync));
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "BackgroundDelete batch task failed");
            throw;
        }
    }

    /// <summary>
    /// Cassandra driver exceptions that indicate the cluster/session is unreachable, as opposed to a single
    /// query timing out against an otherwise healthy cluster. Only these warrant rebuilding the session.
    /// </summary>
    private static bool IsScyllaConnectivityException(Exception ex) =>
        ex is NoHostAvailableException or UnavailableException or OperationTimedOutException;

    /// <summary>
    /// Attempts to heal the Scylla session without letting a healing failure (e.g. a busy self-heal lock)
    /// mask the original exception that triggered the heal attempt.
    /// </summary>
    private async Task TryHealScyllaSessionAsync(string methodName)
    {
        try
        {
            await ScyllaProvider.HealSessionAsync(ScyllaProvider.GetCurrentSessionId(), methodName);
        }
        catch (Exception healEx)
        {
            _logger.LogError(healEx, true, "Scylla session heal attempt failed in {MethodName}", args: methodName);
        }
    }
}