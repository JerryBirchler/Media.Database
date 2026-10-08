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
/// Applies "cdc.public.GroupInvites" change events to Scylla (SCHEMA-35). One CDC record always maps to one
/// Scylla upsert (or delete), same shape as <see cref="GroupsCdcSyncHandler"/>.
/// </summary>
public sealed class GroupInvitesCdcSyncHandler(
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<GroupInvitesCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<GroupInvitesCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.GroupInvites"];

    /// <inheritdoc />
    public async Task ApplyAsync(CdcChangeRecord record, CancellationToken cancellationToken)
    {
        if (record.IsDeleted || record.After is null)
        {
            await DeleteAsync(CdcJson.GetKeyId(record.Key, "GroupInviteId"));
            return;
        }

        await UpsertAsync(record.After.Value);
    }

    private async Task UpsertAsync(JsonElement after)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryGroupInvites.UpsertCql, p =>
            {
                p.AddWithValue(pn.GroupInviteId, after.GetProperty("GroupInviteId").GetInt32());
                p.AddWithValue(pn.GroupInviteUuid, after.GetProperty("GroupInviteUuid").GetGuid());
                p.AddWithValue(pn.GroupId, after.GetProperty("GroupId").GetInt32());
                p.AddWithValue(pn.InvitedByPersonId, after.GetProperty("InvitedByPersonId").GetInt32());
                p.AddWithValue(pn.EmailAddress, after.GetProperty("EmailAddress").GetString()!);
                p.AddWithValue(pn.LastName, after.GetProperty("LastName").GetString()!);
                p.AddWithValue(pn.Status, after.GetProperty("Status").GetInt32());
                p.AddWithValue(pn.ExpiresOn, after.GetProperty("ExpiresOn").GetDateTimeOffset());
                p.AddWithValue(pn.AcceptedByPersonId, after.GetNullableInt32("AcceptedByPersonId")!);
                p.AddWithValue(pn.AnsweredOn, after.GetNullableDateTimeOffset("AnsweredOn")!);
                p.AddWithValue(pn.InsertedOn, after.GetProperty("InsertedOn").GetDateTimeOffset());
                p.AddWithValue(pn.UpdatedOn, after.GetNullableDateTimeOffset("UpdatedOn")!);
            });

            log.LogInformation("CDC upsert applied for GroupInviteId: [{Id}]", after.GetProperty("GroupInviteId").GetInt32());
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying GroupInvites CDC record");
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply GroupInvites CDC record");
            throw;
        }
    }

    private async Task DeleteAsync(int id)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryGroupInvites.DeleteCql, p => p.AddWithValue(pn.GroupInviteId, id));

            log.LogInformation("CDC delete applied for GroupInviteId: [{Id}]", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying GroupInvites CDC delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply GroupInvites CDC delete for GroupInviteId: [{Id}]", id);
            throw;
        }
    }
}
