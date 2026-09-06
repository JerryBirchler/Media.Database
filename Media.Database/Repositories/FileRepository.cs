using Media.Common.Helpers.Fluent;
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
/// PostgreSQL-backed implementation of IFileRepository. Writes go to PostgreSQL synchronously
/// within a transaction; Scylla is kept in sync separately and asynchronously by the CDC
/// pipeline (Media.Common.Cdc.CdcConsumerService dispatching to Cdc.FilesCdcSyncHandler),
/// reading Postgres own write-ahead log rather than this repository writing to both stores.
/// </summary>
public class FileRepository(
    ISqlQueryExecutor sqlExecutor,
    Func<IUnitOfWork> unitOfWorkFactory,
    IMapChangeWordRequests changeWordMapper,
    ILogger<FileRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : IFileRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapChangeWordRequests _changeWordMapper = changeWordMapper;
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
}
