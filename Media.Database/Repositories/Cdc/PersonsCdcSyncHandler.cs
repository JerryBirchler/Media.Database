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
/// Applies "cdc.public.Persons" change events to Scylla. One CDC record always maps to one Scylla
/// upsert (or delete), same shape as <see cref="FilesCdcSyncHandler"/>.
/// </summary>
public sealed class PersonsCdcSyncHandler(
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<PersonsCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<PersonsCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.Persons"];

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
        return document.RootElement.GetProperty("PersonId").GetInt32();
    }

    private async Task UpsertAsync(JsonElement after)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryPersons.UpsertCql, p =>
            {
                p.AddWithValue(pn.PersonId, after.GetProperty("PersonId").GetInt32());
                p.AddWithValue(pn.PersonUuid, after.GetProperty("PersonUuid").GetGuid());
                p.AddWithValue(pn.EmailAddress, after.GetProperty("EmailAddress").GetString()!);
                p.AddWithValue(pn.CellPhoneNumber, after.GetProperty("CellPhoneNumber").GetString()!);
                p.AddWithValue(pn.FirstName, after.GetProperty("FirstName").GetString()!);
                p.AddWithValue(pn.LastName, after.GetProperty("LastName").GetString()!);
                p.AddWithValue(pn.IsActive, after.GetProperty("IsActive").GetBoolean());
                p.AddWithValue(pn.CreatedByPersonId, GetNullableInt32(after, "CreatedByPersonId")!);
                p.AddWithValue(pn.IsSuperAdmin, after.GetProperty("IsSuperAdmin").GetBoolean());
                p.AddWithValue(pn.IsEmailVerified, after.GetProperty("IsEmailVerified").GetBoolean());
                p.AddWithValue(pn.IsSmsVerified, after.GetProperty("IsSmsVerified").GetBoolean());
                p.AddWithValue(pn.OtpWindowOverrideMinutes, GetNullableInt32(after, "OtpWindowOverrideMinutes")!);
                p.AddWithValue(pn.InsertedOn, after.GetProperty("InsertedOn").GetDateTimeOffset());
                p.AddWithValue(pn.UpdatedOn, GetNullableDateTimeOffset(after, "UpdatedOn")!);
            });

            log.LogInformation("CDC upsert applied for PersonId: [{Id}]", after.GetProperty("PersonId").GetInt32());
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Persons CDC record");
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Persons CDC record");
            throw;
        }
    }

    private async Task DeleteAsync(int personId)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryPersons.DeleteCql, p => p.AddWithValue(pn.PersonId, personId));

            log.LogInformation("CDC delete applied for PersonId: [{Id}]", personId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Persons CDC delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Persons CDC delete for PersonId: [{Id}]", personId);
            throw;
        }
    }

    private static int? GetNullableInt32(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetInt32();
    }

    private static DateTimeOffset? GetNullableDateTimeOffset(JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetDateTimeOffset();
    }
}
