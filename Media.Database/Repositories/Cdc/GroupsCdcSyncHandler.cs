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
/// Applies "cdc.public.Groups" change events to Scylla. One CDC record always maps to one Scylla
/// upsert (or delete), same shape as <see cref="FilesCdcSyncHandler"/>.
/// </summary>
public sealed class GroupsCdcSyncHandler(
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<GroupsCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<GroupsCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.Groups"];

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

    private static int ExtractIdFromKey(string key)
    {
        using var document = JsonDocument.Parse(key);
        return document.RootElement.GetProperty("GroupId").GetInt32();
    }

    private async Task UpsertAsync(JsonElement after)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryGroups.UpsertCql, p =>
            {
                p.AddWithValue(pn.GroupId, after.GetProperty("GroupId").GetInt32());
                p.AddWithValue(pn.GroupUuid, after.GetProperty("GroupUuid").GetGuid());
                p.AddWithValue(pn.Name, after.GetProperty("Name").GetString()!);
                p.AddWithValue(pn.Title, after.GetProperty("Title").GetString()!);
                p.AddWithValue(pn.Description, GetNullableString(after, "Description")!);
                p.AddWithValue(pn.IsActive, after.GetProperty("IsActive").GetBoolean());
                p.AddWithValue(pn.IsEncrypted, after.GetProperty("IsEncrypted").GetBoolean());
                p.AddWithValue(pn.InsertedOn, after.GetProperty("InsertedOn").GetDateTimeOffset());
                p.AddWithValue(pn.UpdatedOn, GetNullableDateTimeOffset(after, "UpdatedOn")!);
            });

            log.LogInformation("CDC upsert applied for GroupId: [{Id}]", after.GetProperty("GroupId").GetInt32());
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Groups CDC record");
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Groups CDC record");
            throw;
        }
    }

    private async Task DeleteAsync(int groupId)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryGroups.DeleteCql, p => p.AddWithValue(pn.GroupId, groupId));

            log.LogInformation("CDC delete applied for GroupId: [{Id}]", groupId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Groups CDC delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Groups CDC delete for GroupId: [{Id}]", groupId);
            throw;
        }
    }

    private static string? GetNullableString(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetString();
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetDateTimeOffset();
    }
}
