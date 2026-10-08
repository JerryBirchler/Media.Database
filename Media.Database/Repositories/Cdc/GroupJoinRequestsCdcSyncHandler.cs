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
/// Applies "cdc.public.GroupJoinRequests" change events to Scylla (SCHEMA-35). One CDC record always maps to one
/// Scylla upsert (or delete), same shape as <see cref="GroupsCdcSyncHandler"/>.
/// </summary>
public sealed class GroupJoinRequestsCdcSyncHandler(
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<GroupJoinRequestsCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<GroupJoinRequestsCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.GroupJoinRequests"];

    /// <inheritdoc />
    public async Task ApplyAsync(CdcChangeRecord record, CancellationToken cancellationToken)
    {
        if (record.IsDeleted || record.After is null)
        {
            await DeleteAsync(CdcJson.GetKeyId(record.Key, "GroupJoinRequestId"));
            return;
        }

        await UpsertAsync(record.After.Value);
    }

    private async Task UpsertAsync(JsonElement after)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryGroupJoinRequests.UpsertCql, p =>
            {
                p.AddWithValue(pn.GroupJoinRequestId, after.GetProperty("GroupJoinRequestId").GetInt32());
                p.AddWithValue(pn.GroupJoinRequestUuid, after.GetProperty("GroupJoinRequestUuid").GetGuid());
                p.AddWithValue(pn.GroupId, after.GetProperty("GroupId").GetInt32());
                p.AddWithValue(pn.PersonId, after.GetProperty("PersonId").GetInt32());
                p.AddWithValue(pn.Status, after.GetProperty("Status").GetInt32());
                p.AddWithValue(pn.AnsweredByPersonId, after.GetNullableInt32("AnsweredByPersonId")!);
                p.AddWithValue(pn.AnsweredOn, after.GetNullableDateTimeOffset("AnsweredOn")!);
                p.AddWithValue(pn.InsertedOn, after.GetProperty("InsertedOn").GetDateTimeOffset());
                p.AddWithValue(pn.UpdatedOn, after.GetNullableDateTimeOffset("UpdatedOn")!);
            });

            log.LogInformation("CDC upsert applied for GroupJoinRequestId: [{Id}]", after.GetProperty("GroupJoinRequestId").GetInt32());
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying GroupJoinRequests CDC record");
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply GroupJoinRequests CDC record");
            throw;
        }
    }

    private async Task DeleteAsync(int id)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryGroupJoinRequests.DeleteCql, p => p.AddWithValue(pn.GroupJoinRequestId, id));

            log.LogInformation("CDC delete applied for GroupJoinRequestId: [{Id}]", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying GroupJoinRequests CDC delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply GroupJoinRequests CDC delete for GroupJoinRequestId: [{Id}]", id);
            throw;
        }
    }
}
