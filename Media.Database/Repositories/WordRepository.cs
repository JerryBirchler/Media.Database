using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Npgsql;
using Serilog.Core;
using System.Collections.Concurrent;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IWordRepository"/>.
/// </summary>
public class WordRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<WordRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(scyllaProvider), IWordRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;

    private readonly FluentLogger<WordRepository> _logger = logger.Initializer();

#pragma warning disable S1144
    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;
#pragma warning restore S1144

    /// <inheritdoc/>
    public async Task<Words?> GetByUuid(Guid uuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryWords.GetByUuidSql,
                p => p.AddWithValue(pn.Uuid, uuid),
                reader => reader.ToWord());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByUuid failed for WordUuid: [{Uuid}]", uuid);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetWordsByFileIdOrderedByWord(
        Guid fileId, int sourceMachineId, string? afterWord,
        bool? isCurrent, bool? isProperName, int? limit = 10)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryWords.GetWordsByFileIdOrderedByWordSql,
                p =>
                {
                    p.AddWithValue(pn.FileId, fileId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.Word, (object)afterWord! ?? DBNull.Value);
                    p.AddWithValue(pn.IsCurrent, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isCurrent! ?? DBNull.Value);
                    p.AddWithValue(pn.IsProperName, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isProperName! ?? DBNull.Value);
                    p.AddWithValue(pn.Limit, limit ?? 10);
                },
                reader => reader.ToWordFileWithSourceMachineId());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetWordsByFileIdOrderedByWord failed for FileId: [{FileId}]", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetWordsByFileIdOrderedByOrigin(
        Guid fileId, int sourceMachineId, WordOrigin? afterOrigin,
        bool? isCurrent, bool? isProperName, int? limit = 10)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryWords.GetWordsByFileIdOrderedByOriginSql,
                p =>
                {
                    p.AddWithValue(pn.FileId, fileId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.Origin, (object)afterOrigin! ?? DBNull.Value);
                    p.AddWithValue(pn.IsCurrent, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isCurrent! ?? DBNull.Value);
                    p.AddWithValue(pn.IsProperName, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isProperName! ?? DBNull.Value);
                    p.AddWithValue(pn.Limit, limit ?? 10);
                },
                reader => reader.ToWordFileWithSourceMachineId());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetWordsByFileIdOrderedByOrigin failed for FileId: [{FileId}]", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetWordsByFilePathOrderedByWord(
        string filePath, int sourceMachineId, string? afterWord,
        bool? isCurrent, bool? isProperName, int? limit = 10)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryWords.GetWordsByFilePathOrderedByWordSql,
                p =>
                {
                    p.AddWithValue(pn.OriginalFilePath, filePath);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.Word, (object)afterWord! ?? DBNull.Value);
                    p.AddWithValue(pn.IsCurrent, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isCurrent! ?? DBNull.Value);
                    p.AddWithValue(pn.IsProperName, NpgsqlTypes.NpgsqlDbType.Boolean, (object)isProperName! ?? DBNull.Value);
                    p.AddWithValue(pn.Limit, limit ?? 10);
                },
                reader => reader.ToWordFileWithSourceMachineId());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetWordsByFilePathOrderedByWord failed for FilePath: [{FilePath}]", filePath);
            throw;
        }
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
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            _logger.LogError(ex, "Upsert failed for Word: [{Word}]: ", request.Word);
            throw new BadHttpRequestException($"CameFromFileId: [{request.CameFromFileId}] does not refer to an existing file.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upsert failed for Word: [{Word}]: ", request.Word);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<WordFileIdentifier>> GetFilePageIdentifiers(
        WordFilesOrderBy orderBy, WordFileIdentifier? next, WordOrigin? origin,
        bool? isCurrent, bool? isProperName, int limit)
    {
        var sql = orderBy switch
        {
            WordFilesOrderBy.WordOriginFileId => QueryWords.GetFileIdentifiersByWordOriginSql,
            WordFilesOrderBy.WordFileIdOrigin => QueryWords.GetFileIdentifiersByWordFileIdSql,
            WordFilesOrderBy.FileIdWordOrigin => QueryWords.GetFileIdentifiersByFileIdWordSql,
            WordFilesOrderBy.FileIdOriginWord => QueryWords.GetFileIdentifiersByFileIdOriginSql,
            WordFilesOrderBy.FilePathOrigin => QueryWords.GetFileIdentifiersByFilePathOriginSql,
            WordFilesOrderBy.FilePathWord => QueryWords.GetFileIdentifiersByFilePathWordSql,
            WordFilesOrderBy.WordFilePath => QueryWords.GetFileIdentifiersByWordFilePathSql,
            _ => throw new ArgumentOutOfRangeException(nameof(orderBy), orderBy, "Unrecognized word-files sort order.")
        };

        try
        {
            return await _sqlExecutor.QueryManyAsync(
                sql,
                p =>
                {
                    // Explicit NpgsqlDbType on every optional parameter here is load-bearing, not
                    // stylistic: each one feeds a COALESCE(...) nested inside a multi-column ROW
                    // comparison, and Postgres can't always infer an "unknown"-typed null parameter's
                    // type through that combination (42P08) the way it can for a plain equality.
                    p.AddWithValue(pn.Word, NpgsqlTypes.NpgsqlDbType.Varchar, next?.Word.ToNullableValueForSql() ?? DBNull.Value);
                    p.AddWithValue(pn.FileId, NpgsqlTypes.NpgsqlDbType.Uuid, next?.FileId.ToNullableValueForSql() ?? DBNull.Value);
                    p.AddWithValue(pn.OriginalFilePath, NpgsqlTypes.NpgsqlDbType.Text, next?.OriginalFilePath.ToNullableValueForSql() ?? DBNull.Value);
                    p.AddWithValue(pn.Origin, NpgsqlTypes.NpgsqlDbType.Integer, ((int?)origin).ToNullableValueForSql());
                    p.AddWithValue(pn.IsCurrent, NpgsqlTypes.NpgsqlDbType.Boolean, isCurrent.ToNullableValueForSql());
                    p.AddWithValue(pn.IsProperName, NpgsqlTypes.NpgsqlDbType.Boolean, isProperName.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToWordFileIdentifier());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetFilePageIdentifiers failed for OrderBy {OrderBy}", orderBy);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<ViewWordFiles>> GetByIds(IEnumerable<(int WordId, Guid FileId)> ids, int maxDegreeOfParallelism)
    {
        var results = new ConcurrentBag<ViewWordFiles>();

        await Parallel.ForEachAsync(
            ids,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            async (id, cancellationToken) =>
            {
                var row = await GetByIdPreferringScylla(id.WordId, id.FileId);
                if (row is not null)
                    results.Add(row);
            });

        return [.. results];
    }

    /// <summary>
    /// Looks up a single word/file row by its (WordId, FileId) key, preferring Scylla and falling
    /// back to PostgreSQL when Scylla has no row yet (CDC lag) or is unreachable. A Scylla failure
    /// here is deliberately not rethrown -- PostgreSQL is the durable source of truth, so a
    /// temporarily unavailable Scylla cluster should degrade this request to a normal PostgreSQL
    /// read rather than fail it.
    /// </summary>
    private async Task<ViewWordFiles?> GetByIdPreferringScylla(int wordId, Guid fileId)
    {
        var log = _logger.WithCaller();

        try
        {
            var fromScylla = await _cqlExecutor.QuerySingleAsync(
                QueryWords.GetWordFilesByWordIdAndFileIdCql,
                p =>
                {
                    p.AddWithValue(pn.WordId, wordId);
                    p.AddWithValue(pn.FileId, fileId);
                },
                row => row.ToWordFile());

            if (fromScylla is not null)
                return fromScylla;
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable fetching WordId {WordId}, FileId {FileId}; falling back to Postgres", wordId, fileId);
            await TryHealScyllaSessionAsync(_logger, nameof(GetByIdPreferringScylla));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Scylla lookup failed for WordId {WordId}, FileId {FileId}; falling back to Postgres", wordId, fileId);
        }

        return await _sqlExecutor.QuerySingleAsync(
            QueryWords.GetViewByWordIdAndFileIdSql,
            p =>
            {
                p.AddWithValue(pn.WordId, wordId);
                p.AddWithValue(pn.FileId, fileId);
            },
            reader => reader.ToWordFileWithSourceMachineId());
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
            _logger.LogError(ex, "RefreshView failed: ");
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteByUuid(Guid uuid)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(
                QueryWords.DeleteByUuidSql,
                p => p.AddWithValue(pn.Uuid, uuid));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteByUuid failed for WordUuid: [{Uuid}]:", uuid);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task<List<WordFileLink>> GetWordsByFileId(Guid fileId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QueryWords.GetWordsByFileIdSql,
                p => p.AddWithValue(pn.FileId, fileId),
                reader => reader.ToWordFileLink());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetWordsByFileId failed for FileId: [{FileId}]", fileId);
            throw;
        }
    }

    /// <inheritdoc/>
    public async Task DeleteWordFileLink(Guid fileId, int wordId)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync(
                QueryWords.DeleteWordFileLinkSql,
                p =>
                {
                    p.AddWithValue(pn.FileId, fileId);
                    p.AddWithValue(pn.WordId, wordId);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteWordFileLink failed for FileId: [{FileId}], WordId: [{WordId}]", fileId, wordId);
            throw;
        }
    }
}
