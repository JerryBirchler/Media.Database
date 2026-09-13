using Media.Common.Cdc;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Cdc;

/// <summary>
/// Applies "cdc.public.WordFiles", "cdc.public.Words", and "cdc.public.Files" change events to
/// Scylla's word_files table -- the read-optimized copy of View_WordFiles that lets word/file
/// search pagination hydrate a page from Scylla the same way Files pagination already does (see
/// FileRepository.GetByIds). word_files is a denormalized join across all three source tables
/// (same reasoning as RegistrationsCdcSyncHandler), so a single row-level CDC event can't be
/// applied directly -- instead, this re-queries Postgres's View_WordFiles for whichever rows the
/// event could have affected, and upserts each one. A Words or Files row deletion needs no action
/// here: both WordFiles.WordId and WordFiles.FileId are ON DELETE CASCADE, so Postgres already
/// removed the affected WordFiles rows in the same transaction, which CDC delivers as its own
/// separate "cdc.public.WordFiles" deletion events (same reasoning WordIndexCdcSyncHandler uses
/// for Files deletions).
/// </summary>
public sealed class WordFilesCdcSyncHandler(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<WordFilesCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<WordFilesCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.WordFiles", "cdc.public.Words", "cdc.public.Files"];

    /// <inheritdoc />
    public async Task ApplyAsync(CdcChangeRecord record, CancellationToken cancellationToken)
    {
        switch (record.Topic)
        {
            case "cdc.public.WordFiles":
                await ApplyWordFilesEventAsync(record);
                break;
            case "cdc.public.Words":
                await ApplyWordsEventAsync(record);
                break;
            case "cdc.public.Files":
                await ApplyFilesEventAsync(record);
                break;
        }
    }

    private async Task ApplyWordFilesEventAsync(CdcChangeRecord record)
    {
        if (record.IsDeleted)
        {
            var (wordId, fileId) = ExtractWordFilesKey(record.Key);
            await DeleteAsync(wordId, fileId);
            return;
        }

        if (record.After is null)
            return;

        var afterWordId = record.After.Value.GetProperty("WordId").GetInt32();
        var afterFileId = record.After.Value.GetProperty("FileId").GetGuid();

        var row = await _sqlExecutor.QuerySingleAsync(
            QueryWords.GetViewByWordIdAndFileIdSql,
            p =>
            {
                p.AddWithValue(pn.WordId, afterWordId);
                p.AddWithValue(pn.FileId, afterFileId);
            },
            reader => reader.ToWordFileWithSourceMachineId());

        if (row is not null)
            await UpsertAsync(row);
    }

    private async Task ApplyWordsEventAsync(CdcChangeRecord record)
    {
        if (record.IsDeleted || record.After is null)
            return;

        var wordId = record.After.Value.GetProperty("Id").GetInt32();

        var rows = await _sqlExecutor.QueryManyAsync(
            QueryWords.GetViewByWordIdSql,
            p => p.AddWithValue(pn.WordId, wordId),
            reader => reader.ToWordFileWithSourceMachineId());

        foreach (var row in rows)
            await UpsertAsync(row);
    }

    private async Task ApplyFilesEventAsync(CdcChangeRecord record)
    {
        if (record.IsDeleted || record.After is null)
            return;

        var fileId = record.After.Value.GetProperty("Id").GetGuid();

        var rows = await _sqlExecutor.QueryManyAsync(
            QueryWords.GetViewByFileIdSql,
            p => p.AddWithValue(pn.FileId, fileId),
            reader => reader.ToWordFileWithSourceMachineId());

        foreach (var row in rows)
            await UpsertAsync(row);
    }

    private static (int WordId, Guid FileId) ExtractWordFilesKey(string key)
    {
        using var document = JsonDocument.Parse(key);
        return (
            WordId: document.RootElement.GetProperty("WordId").GetInt32(),
            FileId: document.RootElement.GetProperty("FileId").GetGuid());
    }

    private async Task UpsertAsync(ViewWordFiles wordFile)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryWords.UpsertWordFilesCql, p =>
            {
                p.AddWithValue(pn.WordId, wordFile.WordId);
                p.AddWithValue(pn.FileId, wordFile.FileId);
                p.AddWithValue(pn.Origin, (int)wordFile.Origin);
                p.AddWithValue(pn.Word, wordFile.Word);
                p.AddWithValue(pn.IsCurrent, wordFile.IsCurrent!);
                p.AddWithValue(pn.IsProperName, wordFile.IsProperName!);
                p.AddWithValue(pn.OriginalFilePath, wordFile.OriginalFilePath);
                p.AddWithValue(pn.SourceMachineId, wordFile.SourceMachineId);
                p.AddWithValue(pn.ThumbnailGeneratedOn, wordFile.ThumbnailGeneratedOn!);
            });

            log.LogInformation("CDC upsert applied for WordId {WordId}, FileId {FileId}", wordFile.WordId, wordFile.FileId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying word_files CDC upsert for WordId {WordId}, FileId {FileId}", wordFile.WordId, wordFile.FileId);
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply word_files CDC upsert for WordId {WordId}, FileId {FileId}", wordFile.WordId, wordFile.FileId);
            throw;
        }
    }

    private async Task DeleteAsync(int wordId, Guid fileId)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryWords.DeleteWordFilesCql, p =>
            {
                p.AddWithValue(pn.WordId, wordId);
                p.AddWithValue(pn.FileId, fileId);
            });

            log.LogInformation("CDC delete applied for WordId {WordId}, FileId {FileId}", wordId, fileId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying word_files CDC delete for WordId {WordId}, FileId {FileId}", wordId, fileId);
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply word_files CDC delete for WordId {WordId}, FileId {FileId}", wordId, fileId);
            throw;
        }
    }
}
