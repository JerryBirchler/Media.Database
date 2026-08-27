using Media.Common.Helpers.Fluent;
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
/// PostgreSQL-backed implementation of <see cref="IWordRepository"/>.
/// </summary>
public class WordRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<WordRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository, IWordRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;

    private readonly ILogger<WordRepository> _logger = logger.LogInitializing();

#pragma warning disable S1144
    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;
#pragma warning restore S1144

    /// <inheritdoc/>
    public async Task<Models.Words?> GetById(int id)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryWords.GetByIdSql,
                p => p.AddWithValue(pn.Id, id),
                reader => reader.ToWord());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetById failed for WordId: [{Id}]", id);
            throw;
        }
    }

    /// <summary>
    /// Retrieves a page of word/file rows for the given hand-selected keyset query.
    /// </summary>
    /// <param name="sql">The keyset-paged SQL query to execute; determines the sort order.</param>
    /// <param name="word">The word to search for, or null to match any word.</param>
    /// <param name="origin">The word origin to filter by, or null to match any origin.</param>
    /// <param name="fileId">The file identifier to filter by, or null to match any file.</param>
    /// <param name="isCurrent">Whether to filter to current files only, or null to match any.</param>
    /// <param name="isProperName">Whether to filter to proper names only, or null to match any.</param>
    /// <param name="limit">The maximum number of rows to return.</param>
    /// <returns>The matching word/file rows.</returns>
    public async Task<List<ViewWordFiles>> GetFilePages(
        string sql, string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                sql,
                p =>
                {
                    p.AddWithValue(pn.Word, (object)word! ?? DBNull.Value);
                    p.AddWithValue(pn.Origin, (object)origin! ?? DBNull.Value);
                    p.AddWithValue(pn.FileId, (object)fileId! ?? DBNull.Value);
                    p.AddWithValue(pn.IsCurrent, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isCurrent! ?? DBNull.Value);
                    p.AddWithValue(pn.IsProperName, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isProperName! ?? DBNull.Value);
                    p.AddWithValue(pn.Limit, limit ?? 10);
                },
                reader => reader.ToWordFile());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetFilePages failed for Word: [{Word}], FileId: [{FileId}]", word!, fileId!);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetFilePagesByWordOrigin(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetFilePagesByWordFileId(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetFilePagesByFileIdOrigin(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetFilePagesByFileIdWord(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    /// <inheritdoc/>
    public async Task Upsert(UpsertWordRequest request)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(
                QueryWords.UpsertWordSql,
                p =>
                {
                    p.AddWithValue(pn.Word, request.Word);
                    p.AddWithValue(pn.Origin, (int)request.Origin);
                    p.AddWithValue(pn.IsProperName, request.IsProperName);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow.AdjustPrecision());
                    p.AddWithValue(pn.CameFromFileId, request.CameFromFileId);
                });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "Upsert failed for Word: [{Word}]: ", request.Word);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task RefreshView()
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(QueryWords.RefreshViewSql, static _ => { });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "RefreshView failed: ");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task Delete(int id)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(
                QueryFiles.DeleteSql,
                p => p.AddWithValue(pn.Id, id));
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "Delete failed for WordId: [{Id}]:", id);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteFile(Guid fileId)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(
                QueryWords.DeleteFileSql,
                p => p.AddWithValue(pn.FileId, fileId));
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "DeleteFile failed for FileId: [{FileId}]", fileId);
            throw;
        }
    }
}
