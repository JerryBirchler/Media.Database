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
/// Applies "cdc.public.Registrations" and "cdc.public.SourceMachineRegistrations" change events to
/// Scylla. The Scylla row is a denormalized join across both Postgres tables, so a single row-level
/// CDC event from either table can't be applied directly -- instead, on any event from either
/// topic, this re-queries Postgres for the device's current joined state (the same shape
/// <see cref="QueryRegistrations.GetBySourceMachineUuidSql"/> already returns) and upserts that.
/// Registration volume is low, so the extra Postgres read per event is cheap, and it guarantees
/// correctness by reusing the real join query instead of re-implementing it against raw CDC
/// payloads. Keyed by SourceMachineId, always overwriting the same Scylla row: history lives only
/// in Postgres, per design -- Scylla only ever reflects the current registration for a device.
/// </summary>
public sealed class RegistrationsCdcSyncHandler(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    ILogger<RegistrationsCdcSyncHandler> logger)
    : BaseRepository(scyllaProvider), ICdcSyncHandler
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor ?? throw new ArgumentNullException(nameof(sqlExecutor));
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor ?? throw new ArgumentNullException(nameof(cqlExecutor));
    private readonly FluentLogger<RegistrationsCdcSyncHandler> _logger = logger.Initializer();

    public IReadOnlyList<string> Topics { get; } = ["cdc.public.Registrations", "cdc.public.SourceMachineRegistrations"];

    /// <inheritdoc />
    public async Task ApplyAsync(CdcChangeRecord record, CancellationToken cancellationToken)
    {
        if (record.After is null)
        {
            _logger.WithCaller().LogWarning("Registrations CDC record at offset {Offset} has no After payload; skipping", record.Offset);
            return;
        }

        var sourceMachineId = record.After.Value.GetProperty("SourceMachineId").GetInt32();

        var current = await _sqlExecutor.QuerySingleAsync(
            QueryRegistrations.GetBySourceMachineIdSql,
            p => p.AddWithValue(pn.SourceMachineId, sourceMachineId),
            reader => reader.ToSourceMachineRegistration());

        if (current is null)
        {
            // No current SourceMachineRegistrations+Registrations join exists for this device --
            // the raw Registrations table has no SourceMachineUuid column of its own (only
            // SourceMachineRegistrations does), and Scylla's Registrations table is partitioned by
            // SourceMachineUuid, so there is no reliable key to target a Scylla delete with here.
            // In practice nothing in the application ever physically deletes a Registrations row
            // (only inserts and IsCurrent updates), so this should not occur outside manual data
            // surgery -- log it rather than guess at a partition key.
            _logger.WithCaller().LogWarning(
                "No current registration join found for SourceMachineId {SourceMachineId}; leaving Scylla untouched",
                sourceMachineId);
            return;
        }

        await UpsertAsync(current);
    }

    private async Task UpsertAsync(SourceMachineRegistrations registration)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryRegistrations.UpsertRegistrationCql, p =>
            {
                p.AddWithValue(pn.RegistrationId, registration.RegistrationId);
                p.AddWithValue(pn.SourceMachineId, registration.SourceMachineId);
                p.AddWithValue(pn.SourceMachineUuid, registration.SourceMachineUuid);
                p.AddWithValue(pn.SourceMachineName, registration.SourceMachineName);
                p.AddWithValue(pn.DeviceTypeId, (int)registration.DeviceTypeId);
                p.AddWithValue(pn.FirstName, registration.FirstName);
                p.AddWithValue(pn.LastName, registration.LastName);
                p.AddWithValue(pn.EmailAddress, registration.EmailAddress);
                p.AddWithValue(pn.CellPhoneNumber, registration.CellPhoneNumber);
                p.AddWithValue(pn.OperatingSystem, registration.OperatingSystem);
                p.AddWithValue(pn.SourceInsertedOn, registration.InsertedOn);
                p.AddWithValue(pn.SourceUpdatedOn, registration.UpdatedOn!);
                p.AddWithValue(pn.IsActive, registration.IsActive);
                p.AddWithValue(pn.OtpEmail, registration.OtpEmail);
                p.AddWithValue(pn.OtpCellPhone, registration.OtpCellPhone);
                p.AddWithValue(pn.IsEmailVerified, registration.IsEmailVerified);
                p.AddWithValue(pn.IsSmsVerified, registration.IsSmsVerified);
                p.AddWithValue(pn.RegistrationInsertedOn, registration.RegistrationInsertedOn!);
                p.AddWithValue(pn.RegistrationUpdatedOn, registration.RegistrationUpdatedOn!);
            });

            log.LogInformation("CDC upsert applied for SourceMachineId {SourceMachineId}", registration.SourceMachineId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable applying Registrations CDC record for SourceMachineId {SourceMachineId}", registration.SourceMachineId);
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to apply Registrations CDC record for SourceMachineId {SourceMachineId}", registration.SourceMachineId);
            throw;
        }
    }

}
