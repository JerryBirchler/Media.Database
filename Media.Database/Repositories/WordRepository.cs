using Media.Common.Helpers;
using Media.Common.Providers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using Serilog.Core;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

public class WordRepository(
    IPostgresConnectionProvider postgresProvider,
    ILogger<WordRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(postgresProvider), IWordRepository
{
    private readonly ILogger<WordRepository> _logger = (new Func<ILogger<WordRepository>>(() =>
    {
        var className = ClassHelper.GetName();
        logger.LogInformation(ClassHelper.Initializing, className);
        return logger;
    })());

#pragma warning disable S1144
    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;
#pragma warning restore S1144

    public async Task<Models.Words?> GetById(int id)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryWords.GetByIdSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            await using var reader = await sqlCommand.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
                return null;

            return reader.ToWord();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "GetById failed for WordId: [{Id}]", args: id);
            throw;
        }
    }

    public async Task<List<ViewWordFiles>> GetFilePages(
        string sql, string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(sql);
            sqlCommand.Parameters.AddWithValue(pn.Word, (object)word! ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(pn.Origin, (object)origin! ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(pn.FileId, (object)fileId! ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(pn.IsCurrent, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isCurrent! ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(pn.IsProperName, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isProperName! ?? DBNull.Value);
            sqlCommand.Parameters.AddWithValue(pn.Limit, limit ?? 10);
            await using var reader = await sqlCommand.ExecuteReaderAsync();
            return await reader.ToWordFiles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "GetFilePages failed for Word: [{Word}], FileId: [{FileId}]", args: [word!, fileId!]);
            throw;
        }
    }

    public async Task<List<ViewWordFiles>> GetFilePagesByWordOrigin(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task<List<ViewWordFiles>> GetFilePagesByWordFileId(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task<List<ViewWordFiles>> GetFilePagesByFileIdOrigin(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task<List<ViewWordFiles>> GetFilePagesByFileIdWord(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task Upsert(UpsertWordRequest request)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryWords.UpsertWordSql);
            sqlCommand.Parameters.AddWithValue(pn.Word, request.Word);
            sqlCommand.Parameters.AddWithValue(pn.Origin, (int)request.Origin);
            sqlCommand.Parameters.AddWithValue(pn.IsProperName, request.IsProperName);
            sqlCommand.Parameters.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
            sqlCommand.Parameters.AddWithValue(pn.CameFromFileId, request.CameFromFileId);
            await sqlCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "Upsert failed for Word: [{Word}]: ", args: request.Word);
            throw;
        }
    }

    public async Task RefreshView()
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryWords.RefreshViewSql);
            await sqlCommand.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "RefreshView failed: ");
            throw;
        }
    }

    public async Task Delete(int id)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteSql);
            sqlCommand.Parameters.AddWithValue(pn.Id, id);
            await sqlCommand.ExecuteReaderAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "Delete failed for WordId: [{Id}]:", args: id);
            throw;
        }
    }

    public async Task DeleteFile(Guid fileId)
    {
        try
        {
            await using var sqlConnection = GetSqlConnection();
            await using var sqlCommand = await sqlConnection.GetCommand(QueryWords.DeleteFileSql);
            sqlCommand.Parameters.AddWithValue(pn.FileId, fileId);
            await sqlCommand.ExecuteReaderAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, true, "DeleteFile failed for FileId: [{FileId}]", args: fileId);
            throw;
        }
    }
}
