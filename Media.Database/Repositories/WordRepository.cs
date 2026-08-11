using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Configuration;

#pragma warning disable CS8981 
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981 

namespace Media.Database.Repositories;

public class WordRepository(IConfiguration configuration) 
    : BaseRepository(configuration), IWordRepository
{
    public async Task<Models.Words?> GetById(int id)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryWords.GetByIdSql);
        sqlCommand.Parameters.AddWithValue(pn.Id, id);
        await using var reader = await sqlCommand.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return reader.ToWord();
    }

    public async Task<List<Models.ViewWordFiles>> GetFilePages(
        string sql, string? word, WordOrigin? origin, Guid? fileId, 
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
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

    public async Task<List<Models.ViewWordFiles>> GetFilePagesByWordOrigin(
        string? word, WordOrigin? origin, Guid? fileId, 
        bool? isCurrent, bool? isProperName, 
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task<List<Models.ViewWordFiles>> GetFilePagesByWordFileId(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task<List<Models.ViewWordFiles>> GetFilePagesByFileIdOrigin(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task<List<Models.ViewWordFiles>> GetFilePagesByFileIdWord(
        string? word, WordOrigin? origin, Guid? fileId,
        bool? isCurrent, bool? isProperName,
        int? limit = 10)
    {
        return await GetFilePages(QueryWords.GetFilePagesByWordFileIdSql, word, origin, fileId, isCurrent, isProperName, limit);
    }

    public async Task Upsert(UpsertWordRequest request)
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

    public async Task RefreshView()
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryWords.RefreshViewSql);
        await sqlCommand.ExecuteNonQueryAsync();
    }

    public async Task Delete(int id)
    {
        await using var sqlConnection = GetSqlConnection();
        await using var sqlCommand = await sqlConnection.GetCommand(QueryFiles.DeleteSql);
        sqlCommand.Parameters.AddWithValue(pn.Id, id);
        await sqlCommand.ExecuteReaderAsync();
    }
}