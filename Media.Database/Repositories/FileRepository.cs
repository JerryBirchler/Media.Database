using Media.Common.Helpers;
using Media.Database.Helpers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981 

namespace Media.Database.Repositories;

public class FileRepository(
    ILogger<FileRepository> logger)
    : BaseRepository(), IFileRepository
{
    private readonly ILogger<FileRepository> _logger = (new Func<ILogger<FileRepository>>(() =>
    {
        var className = ClassHelper.GetName();
        logger.LogInformation("class: [{ClassName}] initializing", className);
        return logger;
    })());

    private readonly int _scyllaMaxBatchsize = BaseStartup.ScyllaSettings!.MaxBatchsize;

    public async Task<Files?> GetById(Guid id)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetByIdSql);
        sqlCommand.Parameters.AddWithValue(pn.Id, id);
        await using var reader = await sqlCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return reader.ToFile();
    }

    public async Task<Files?> GetCurrentBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
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

    public async Task<List<Models.Files>> GetCurrentPagesBySourceMachineId(int sourceMachineId, string? originalFilePath, int limit = 5)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetCurrentPagesBySourceMachineIdSql);
        sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
        sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath.ToNullableValueForSql());
        sqlCommand.Parameters.AddWithValue(pn.Limit, limit);
        await using var reader = await sqlCommand.ExecuteReaderAsync();
        return await reader.ToFiles();
    }

    public async Task<List<Models.Files>> GetHistoryPagesBySourceMachineId(int sourceMachineId, string originalFilePath, int limit = 5)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetHistoryPagesBySourceMachineIdSql);
        sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
        sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath);
        sqlCommand.Parameters.AddWithValue(pn.Limit, limit);
        await using var reader = await sqlCommand.ExecuteReaderAsync();
        return await reader.ToFiles();
    }

    public async Task<Models.Files?> Upsert(UploadFileRequest request)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.ExistsSql);
        sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
        sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
        sqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, (object)request.LastFileUpdate! ?? DBNull.Value);

        await using (var reader = await sqlCommand.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                return new Files
                {
                    Id = reader.ToId(),
                    Exists = true
                };
            }
        }

        await sqlCommand.DisposeAsync();

        List<Guid> previousIds = [];
        await using var sqlCommand2 = await sqlConnection.GetCommand(QueryFiles.GetPreviousIdsSql);
        sqlCommand2.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
        sqlCommand2.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);

        await using (var reader2 = await sqlCommand2.ExecuteReaderAsync())
            previousIds = await reader2.ToIds();
        await sqlCommand2.DisposeAsync();

        await using var sqlCommand3 = await sqlConnection.GetCommand(QueryFiles.UpsertSql);
        sqlCommand3.Parameters.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
        sqlCommand3.Parameters.AddWithValue(pn.OriginalFilePath, request.OriginalFilePath);
        sqlCommand3.Parameters.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
        sqlCommand3.Parameters.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
        sqlCommand3.Parameters.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);
        await using var reader3 = await sqlCommand3.ExecuteReaderAsync();

        if (!await reader3.ReadAsync())
            return null;

        var file = reader3.ToFile();

        await sqlCommand3.DisposeAsync();
        await sqlConnection.CloseAsync();

        BackgroundUpdate(file, previousIds);
        return file;
    }

    public async Task<UpdateFileResponse> Update(
        Guid id,
        UpdateFileRequest request)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.GetByIdSql);
        sqlCommand.Parameters.AddWithValue(pn.Id, id);

        Files currentFile = null!;

        await using (var reader = await sqlCommand.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
                return new UpdateFileResponse { File = null };

            currentFile = reader.ToFile();
        }

        UpdateFileResponse response = new()
        {
            Updates = GetUpdates(currentFile, request)
        };

        await using var sqlCommand2 = await sqlConnection.GetCommand(QueryFiles.UpdateSql);
        sqlCommand2.Parameters.AddWithValue(pn.Id, id);
        sqlCommand2.Parameters.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
        sqlCommand2.Parameters.AddWithValue(pn.LastFileUpdate, request.LastFileUpdate.AdjustPrecision().ToNullableValueForSql());
        sqlCommand2.Parameters.AddWithValue(pn.Metadata, NpgsqlTypes.NpgsqlDbType.Json, request.Metadata.ToNullableValueForSql()?.ToJsonString()!);

        await using (var reader2 = await sqlCommand2.ExecuteReaderAsync())
        {
            if (!await reader2.ReadAsync())
                return response;

            response.File = reader2.ToFile();
        }

        BackgroundUpdate(response.File);
        return response;
    }

    private static List<ChangeWordRequest> GetUpdates(Files current, UpdateFileRequest request)
    {
        var updates = new List<ChangeWordRequest>();
        var curMeta = current.Metadata;
        var newMeta = request.Metadata;

        if (curMeta is null && newMeta is null)
            return updates;

        updates.ProcessList(curMeta?.Names, newMeta?.Names, current, WordOrigin.Name);
        updates.ProcessList(curMeta?.KeyWords, newMeta?.KeyWords, current, WordOrigin.Keyword);

        updates.ProcessScalar(curMeta?.Title, newMeta?.Title, current, WordOrigin.FromTitle);
        updates.ProcessScalar(curMeta?.Description, newMeta?.Description, current, WordOrigin.FromDescription);
        updates.ProcessScalar(curMeta?.Event, newMeta?.Event, current, WordOrigin.FromEvent);
        updates.ProcessScalar(curMeta?.Location, newMeta?.Location, current, WordOrigin.FromLocation);

        return updates;
    }

    private void BackgroundUpdate(Files file, List<Guid> previousIds)
    {
        _ = Task.Run(async () =>
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, true, $"BackgroundUpdate task failed for FileId {file.Id}");
            }
        });
    }

    private void BackgroundUpdate(Files file)
    {
        _ = Task.Run(async () =>
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, true, $"BackgroundUpdate task failed for FileId {file.Id}");
            }
        });
    }


    public async Task<Models.Files?> Delete(Guid id)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteSql);
        sqlCommand.Parameters.AddWithValue(pn.Id, id);
        await using var reader = await sqlCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        var file = reader.ToFile();

        BackgroundDelete(id);
        return file;
    }

    public async Task<List<Models.Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteHistorySql);
        sqlCommand.Parameters.AddWithValue(pn.SourceMachineId, sourceMachineId);
        sqlCommand.Parameters.AddWithValue(pn.OriginalFilePath, originalFilePath);
        await using var reader = await sqlCommand.ExecuteReaderAsync();

        var files = await reader.ToFiles();

        if (files.Count > 0)
            BackgroundDelete(files);

        return files;
    }

    private void BackgroundDelete(Guid id)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var noSqlConnection = GetNoSqlConnection();
                var noSqlCommand = noSqlConnection.GetNoSqlCommand(QueryFiles.DeleteNoSql);

                noSqlCommand.Parameters.AddWithValue(pn.Id, id);
                await noSqlCommand.ExecuteRowSet();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, true, $"BackgroundDelete task failed for FileId {id}");
            }
        });
    }

    private void BackgroundDelete(List<Files> files)
    {
        _ = Task.Run(async () =>
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, true, $"BackgroundDelete task failed");
            }
        });
    }
}