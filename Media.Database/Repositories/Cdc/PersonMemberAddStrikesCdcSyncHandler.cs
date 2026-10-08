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
/// Applies "cdc.public.PersonMemberAddStrikes" change events to Scylla (SCHEMA-35). One CDC record always maps to one
/// Scylla upsert (or delete), same shape as <see cref="GroupsCdcSyncHandler"/>.
/// </summary>
public sealed class PersonMemberAddStrikesCdcSyncHandler(
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<PersonMemberAddStrikesCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<PersonMemberAddStrikesCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.PersonMemberAddStrikes"];

    /// <inheritdoc />
    public async Task ApplyAsync(CdcChangeRecord record, CancellationToken cancellationToken)
    {
        if (record.IsDeleted || record.After is null)
        {
            await DeleteAsync(CdcJson.GetKeyId(record.Key, "PersonId"));
            return;
        }

        await UpsertAsync(record.After.Value);
    }

    private async Task UpsertAsync(JsonElement after)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryPersonMemberAddStrikes.UpsertCql, p =>
            {
                p.AddWithValue(pn.PersonId, after.GetProperty("PersonId").GetInt32());
                p.AddWithValue(pn.StrikeCount, after.GetProperty("StrikeCount").GetInt32());
                p.AddWithValue(pn.WindowEndsOn, after.GetNullableDateTimeOffset("WindowEndsOn")!);
                p.AddWithValue(pn.LockedOn, after.GetNullableDateTimeOffset("LockedOn")!);
                p.AddWithValue(pn.InsertedOn, after.GetProperty("InsertedOn").GetDateTimeOffset());
                p.AddWithValue(pn.UpdatedOn, after.GetNullableDateTimeOffset("UpdatedOn")!);
            });

            log.LogInformation("CDC upsert applied for PersonId: [{Id}]", after.GetProperty("PersonId").GetInt32());
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying PersonMemberAddStrikes CDC record");
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply PersonMemberAddStrikes CDC record");
            throw;
        }
    }

    private async Task DeleteAsync(int id)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryPersonMemberAddStrikes.DeleteCql, p => p.AddWithValue(pn.PersonId, id));

            log.LogInformation("CDC delete applied for PersonId: [{Id}]", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying PersonMemberAddStrikes CDC delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply PersonMemberAddStrikes CDC delete for PersonId: [{Id}]", id);
            throw;
        }
    }
}
