using Media.Common.Cdc;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories.Cdc;

/// <summary>
/// Applies "cdc.public.Files" change events to Scylla. One CDC record always maps to one Scylla
/// upsert (or delete) -- no batch/"previous version" special-casing is needed here, because
/// Postgres's own <c>GetPreviousIdsSql</c> (run inside the same transaction as a new file version's
/// insert) already flips a superseded version's own IsCurrent to false as a normal row update,
/// which CDC delivers as its own separate change event for that row.
/// </summary>
public sealed class FilesCdcSyncHandler(
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<FilesCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<FilesCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.Files"];

    /// <inheritdoc />
    public async Task ApplyAsync(CdcChangeRecord record, CancellationToken cancellationToken)
    {
        if (record.IsDeleted || record.After is null)
        {
            await DeleteAsync(ExtractIdFromKey(record.Key));
            return;
        }

        await UpsertAsync(record.After.Value);
    }

    private static Guid ExtractIdFromKey(string key)
    {
        using var document = JsonDocument.Parse(key);
        return document.RootElement.GetProperty("Id").GetGuid();
    }

    private async Task UpsertAsync(JsonElement after)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryFiles.UpsertCql, p =>
            {
                p.AddWithValue(pn.Id, after.GetProperty("Id").GetGuid());
                p.AddWithValue(pn.SourceMachineId, after.GetProperty("SourceMachineId").GetInt32());
                p.AddWithValue(pn.OriginalFilePath, after.GetProperty("OriginalFilePath").GetString()!);
                p.AddWithValue(pn.InsertedOn, after.GetProperty("InsertedOn").GetDateTimeOffset());
                p.AddWithValue(pn.UpdatedOn, after.GetProperty("UpdatedOn").GetDateTimeOffset());
                p.AddWithValue(pn.LastFileUpdate, after.GetProperty("LastFileUpdate").GetDateTimeOffset());
                p.AddWithValue(pn.IsCurrent, after.GetProperty("IsCurrent").GetBoolean());
                p.AddWithValue(pn.Metadata, GetNullableString(after, "Metadata")!);
            });

            log.LogInformation("CDC upsert applied for FileId {Id}", after.GetProperty("Id").GetGuid());
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Files CDC record");
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Files CDC record");
            throw;
        }
    }

    private async Task DeleteAsync(Guid id)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryFiles.DeleteCql, p => p.AddWithValue(pn.Id, id));

            log.LogInformation("CDC delete applied for FileId {Id}", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Files CDC delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Files CDC delete for FileId {Id}", id);
            throw;
        }
    }

    private static string? GetNullableString(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetString();
    }
}
