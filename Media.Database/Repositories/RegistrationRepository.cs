using Media.Database.Helpers;
using Media.Common.Helpers;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Common.Settings;
using Media.Common.Transactions;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Npgsql;
using Serilog.Core;
using System.Collections.Concurrent;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
using os = Media.Database.Repositories.Schemas.OrdinalsSql;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of IRegistrationRepository. Writes go to PostgreSQL
/// synchronously; Scylla is kept in sync separately and asynchronously by the CDC pipeline
/// (Media.Common.Cdc.CdcConsumerService dispatching to Cdc.RegistrationsCdcSyncHandler),
/// reading Postgres own write-ahead log rather than this repository writing to both stores.
/// <see cref="GetByIdsAsync"/> hydrates preferring that same Scylla table, falling back to
/// PostgreSQL per row -- same split as <see cref="GroupRepository.GetByIdsAsync"/>.
/// </summary>
public class RegistrationRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    Func<IUnitOfWork> unitOfWorkFactory,
    IOptions<RegistrationSettings> registrationSettings,
    ILogger<RegistrationRepository> logger,
    IMapRegistrationResponses registrationResponseMapper,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(scyllaProvider), IRegistrationRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly IOptions<RegistrationSettings> _registrationSettings = registrationSettings;
    private readonly FluentLogger<RegistrationRepository> _logger = logger.Initializer();
    private readonly IMapRegistrationResponses _registrationResponseMapper = registrationResponseMapper;

    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;

    public async Task<SourceMachineRegistrations?> AddBySourceInformation(AddSourceInformationRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var addSourceResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.GetBySourceInformationSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineName, request.SourceMachineName);
                    p.AddWithValue(pn.DeviceTypeId, (int)request.DeviceTypeId);
                    p.AddWithValue(pn.FirstName, request.FirstName);
                    p.AddWithValue(pn.LastName, request.LastName);
                },
                reader => reader.ToSourceMachineRegistration()
            );

            // An existing device re-registering with a changed email/phone/OS needs its stored
            // contact info actually updated -- otherwise verification later would keep checking
            // the OLD email/phone (see VerifyOtpEmailSql/VerifyOtpCellPhoneSql, which match against
            // this row's own stored values). The fresh Registrations row inserted further down
            // already resets IsEmailVerified/IsSmsVerified to false regardless, so a changed,
            // previously-verified channel is naturally re-verified with no extra logic needed here.
            if (addSourceResponse is not null &&
                (addSourceResponse.EmailAddress != request.EmailAddress ||
                 addSourceResponse.CellPhoneNumber != request.CellPhoneNumber ||
                 addSourceResponse.OperatingSystem != request.OperatingSystem))
            {
                var baseline = addSourceResponse;
                addSourceResponse = await _sqlExecutor.QuerySingleAsync
                (
                    uow,
                    QueryRegistrations.UpdateSourceInformationSql,
                    p =>
                    {
                        p.AddWithValue(pn.SourceMachineUuid, baseline.SourceMachineUuid);
                        p.AddWithValue(pn.EmailAddress, request.EmailAddress);
                        p.AddWithValue(pn.CellPhoneNumber, request.CellPhoneNumber);
                        p.AddWithValue(pn.OperatingSystem, request.OperatingSystem);
                    },
                    reader => reader.ToSourceMachineRegistration(baseline)
                );
            }

            // No existing row matched — this is a genuinely new device. GetBySourceInformationSql
            // is a lookup only; it never creates a row, so a brand-new device must be inserted here.
            // DisambiguationKey is not unique on its own -- only (SourceMachineName, DeviceTypeId,
            // DisambiguationKey) together is -- so a collision is retried with a freshly generated
            // key rather than treated as a real failure. The keyspace (62^5) makes more than one or
            // two retries astronomically unlikely; the bounded loop is a safety net, not an expected
            // path.
            if (addSourceResponse is null)
            {
                const int maxAttempts = 5;

                for (var attempt = 1; ; attempt++)
                {
                    try
                    {
                        addSourceResponse = await _sqlExecutor.QuerySingleAsync
                        (
                            QueryRegistrations.AddBySourceInformationSql,
                            p =>
                            {
                                p.AddWithValue(pn.SourceMachineName, request.SourceMachineName);
                                p.AddWithValue(pn.DeviceTypeId, (int)request.DeviceTypeId);
                                p.AddWithValue(pn.DisambiguationKey, DisambiguationKey.Generate());
                                p.AddWithValue(pn.EmailAddress, request.EmailAddress);
                                p.AddWithValue(pn.CellPhoneNumber, request.CellPhoneNumber);
                                p.AddWithValue(pn.FirstName, request.FirstName);
                                p.AddWithValue(pn.LastName, request.LastName);
                                p.AddWithValue(pn.OperatingSystem, request.OperatingSystem);
                            },
                            reader => reader.ToNewSourceMachineRegistration()
                        );

                        break;
                    }
                    catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.UniqueViolation
                        && ex.ConstraintName == "IX_SourceMachineRegistrations_Name_DeviceType_DisambiguationKey"
                        && attempt < maxAttempts)
                    {
                        _logger.LogWarning("DisambiguationKey collision on attempt {Attempt}/{MaxAttempts} for SourceMachineName {SourceMachineName}", attempt, maxAttempts, request.SourceMachineName);
                    }
                }
            }

            if (addSourceResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            await _sqlExecutor.QueryManyAsync
            (
                uow,
                QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, addSourceResponse.SourceMachineUuid);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToRegistrationIds()
            );

            var otpEmail = OneTimePassword.Generate();
            var otpCellPhone = OneTimePassword.Generate(excluding: otpEmail);

            var addRegistrationResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.AddRegistrationBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, addSourceResponse.SourceMachineUuid);
                    p.AddWithValue(pn.OtpEmail, otpEmail);
                    p.AddWithValue(pn.OtpCellPhone, otpCellPhone);
                },
                reader => reader.ToAddRegistrationResponse()
            );

            if (addRegistrationResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            addSourceResponse = addSourceResponse with { HasRegistration = true };
            addSourceResponse.OtpEmail = addRegistrationResponse.OtpEmail;
            addSourceResponse.OtpCellPhone = addRegistrationResponse.OtpCellPhone;
            addSourceResponse.RegistrationId = addRegistrationResponse.Id;
            addSourceResponse.RegistrationInsertedOn = addRegistrationResponse.InsertedOn;
            addSourceResponse.RegistrationUpdatedOn = addRegistrationResponse.UpdatedOn;

            await uow.CommitAsync();

            return addSourceResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddBySourceInformation failed for SourceMachineName {SourceMachineName}", request.SourceMachineName);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<SourceMachineRegistrations?> GetBySourceInformationAsync(string sourceMachineName, DeviceTypes deviceTypeId, string firstName, string lastName)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryRegistrations.GetBySourceInformationSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineName, sourceMachineName);
                    p.AddWithValue(pn.DeviceTypeId, (int)deviceTypeId);
                    p.AddWithValue(pn.FirstName, firstName);
                    p.AddWithValue(pn.LastName, lastName);
                },
                reader => reader.ToSourceMachineRegistration());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBySourceInformationAsync failed for {SourceMachineName}", sourceMachineName);
            throw;
        }
    }

    public async Task<SourceMachineRegistrations?> GetByUuid(Guid uuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.GetBySourceMachineUuidSql,
                p => p.AddWithValue(pn.SourceMachineUuid, uuid),
                reader => reader.ToSourceMachineRegistration()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByUuid failed for SourceMachineUuid {Uuid}", uuid);
            throw;
        }
    }

    public async Task<SourceMachineRegistrations?> GetByPersonSourceMachineUuid(Guid uuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.GetByPersonSourceMachineUuidSql,
                p => p.AddWithValue(pn.PersonSourceMachineUuid, uuid),
                reader => reader.ToSourceMachineRegistrationViaPerson()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByPersonSourceMachineUuid failed for PersonSourceMachineUuid {Uuid}", uuid);
            throw;
        }
    }

    /// <summary>
    /// Updates the source information for a given source machine UUID. If the email address or cell phone number has changed,
    /// a new registration is created and the old one is inactivated.
    /// </summary>
    /// <param name="request">The request containing the updated source information.</param>
    /// <returns>The updated source machine registration, or null if the registration does not exist.</returns>
    public async Task<SourceMachineRegistrations?> UpdateSourceInformation(UpdateSourceInformationRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var existingRegistration = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.GetBySourceMachineIdSql,
                p => p.AddWithValue(pn.SourceMachineId, request.SourceMachineId),
                reader => reader.ToSourceMachineRegistration()
            );

            if (existingRegistration is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            var updateResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.UpdateSourceInformationSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, request.SourceMachineId);
                    p.AddWithValue(pn.EmailAddress, request.EmailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, request.CellPhoneNumber);
                    p.AddWithValue(pn.OperatingSystem, request.OperatingSystem);
                },
                reader => reader.ToSourceMachineRegistration(existingRegistration)
            );

            if (updateResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            if (existingRegistration.EmailAddress == request.EmailAddress
                && existingRegistration.CellPhoneNumber == request.CellPhoneNumber)
            {
                await uow.CommitAsync();
                return updateResponse;
            }

            var ids = await _sqlExecutor.QueryManyAsync
            (
                uow,
                QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, existingRegistration.SourceMachineUuid);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToRegistrationIds()
            );

            var otpEmail = existingRegistration.IsEmailVerified ? string.Empty : OneTimePassword.Generate();
            var otpCellPhone = existingRegistration.IsSmsVerified ? string.Empty : OneTimePassword.Generate(excluding: otpEmail);

            var addRegistrationResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.AddRegistrationBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, existingRegistration.SourceMachineUuid);
                    p.AddWithValue(pn.OtpEmail, otpEmail);
                    p.AddWithValue(pn.OtpCellPhone, otpCellPhone);
                },
                reader => reader.ToAddRegistrationResponse()
            );

            if (addRegistrationResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            updateResponse.OtpEmail = addRegistrationResponse.OtpEmail;
            updateResponse.OtpCellPhone = addRegistrationResponse.OtpCellPhone;
            updateResponse.RegistrationId = addRegistrationResponse.Id;
            updateResponse.RegistrationInsertedOn = addRegistrationResponse.InsertedOn;
            updateResponse.RegistrationUpdatedOn = addRegistrationResponse.UpdatedOn;

            await uow.CommitAsync();

            return updateResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSourceInformation failed for SourceMachineId {SourceMachineId}", request.SourceMachineId);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<ResendOtpResult?> ResendOtp(string sourceMachineName, DeviceTypes deviceTypeId, string emailAddress, string cellPhoneNumber)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var existingRegistration = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.GetBySourceInformationSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineName, sourceMachineName);
                    p.AddWithValue(pn.DeviceTypeId, (int)deviceTypeId);
                    p.AddWithValue(pn.EmailAddress, emailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, cellPhoneNumber);
                },
                reader => reader.ToSourceMachineRegistration()
            );

            if (existingRegistration is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            if (existingRegistration.IsEmailVerified && existingRegistration.IsSmsVerified)
            {
                await uow.CommitAsync();
                return new ResendOtpResult
                {
                    EmailOtpSent = false,
                    SmsOtpSent = false,
                    OtpEmail = string.Empty
                };
            }

            await _sqlExecutor.QueryManyAsync
            (
                uow,
                QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, existingRegistration.SourceMachineUuid);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToRegistrationIds()
            );

            var otpEmail = existingRegistration.IsEmailVerified ? string.Empty : OneTimePassword.Generate();
            var otpCellPhone = existingRegistration.IsSmsVerified ? string.Empty : OneTimePassword.Generate(excluding: otpEmail);

            var addRegistrationResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.AddRegistrationBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, existingRegistration.SourceMachineUuid);
                    p.AddWithValue(pn.OtpEmail, otpEmail);
                    p.AddWithValue(pn.OtpCellPhone, otpCellPhone);
                },
                reader => reader.ToAddRegistrationResponse()
            );

            if (addRegistrationResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            await uow.CommitAsync();

            return new ResendOtpResult
            {
                EmailOtpSent = !existingRegistration.IsEmailVerified,
                SmsOtpSent = !existingRegistration.IsSmsVerified,
                OtpEmail = addRegistrationResponse.OtpEmail
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResendOtp failed for SourceMachineName {SourceMachineName}", sourceMachineName);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<OtpEmailResponse?> VerifyOtpEmail(string emailAddress, string sourceMachineName, DeviceTypes deviceTypeId, string otp)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.VerifyOtpEmailSql,
                p =>
                {
                    p.AddWithValue(pn.EmailAddress, emailAddress);
                    p.AddWithValue(pn.SourceMachineName, sourceMachineName);
                    p.AddWithValue(pn.DeviceTypeId, (int)deviceTypeId);
                    p.AddWithValue(pn.OtpEmail, otp);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                    p.AddWithValue(pn.OtpWindowStart, DateTimeOffset.UtcNow - _registrationSettings.Value.OtpWindow);
                },
                reader => _registrationResponseMapper.ToOtpEmailResponse(
                    reader.GetGuid(os.SourceMachineUuid),
                    reader.GetString(os.SourceMachineName),
                    (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
                    reader.GetString(os.DisambiguationKey),
                    reader.GetString(os.FirstName),
                    reader.GetString(os.LastName),
                    reader.GetString(os.EmailAddress),
                    reader.GetString(os.CellPhoneNumber),
                    reader.GetFieldValue<bool>(os.IsEmailVerified),
                    reader.GetFieldValue<bool>(os.IsSmsVerified))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyOtpEmail failed for EmailAddress {EmailAddress}", emailAddress);
            throw;
        }
    }

    public async Task<OtpSmsResponse?> VerifyOtpCellPhone(string cellPhoneNumber, string sourceMachineName, DeviceTypes deviceTypeId, string otp)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.VerifyOtpCellPhoneSql,
                p =>
                {
                    p.AddWithValue(pn.CellPhoneNumber, cellPhoneNumber);
                    p.AddWithValue(pn.SourceMachineName, sourceMachineName);
                    p.AddWithValue(pn.DeviceTypeId, (int)deviceTypeId);
                    p.AddWithValue(pn.OtpCellPhone, otp);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                    p.AddWithValue(pn.OtpWindowStart, DateTimeOffset.UtcNow - _registrationSettings.Value.OtpWindow);
                },
                reader => _registrationResponseMapper.ToOtpSmsResponse(
                    reader.GetGuid(os.SourceMachineUuid),
                    reader.GetString(os.SourceMachineName),
                    (DeviceTypes)reader.GetInt32(os.DeviceTypeId),
                    reader.GetString(os.DisambiguationKey),
                    reader.GetString(os.FirstName),
                    reader.GetString(os.LastName),
                    reader.GetString(os.CellPhoneNumber),
                    reader.GetFieldValue<bool>(os.IsSmsVerified),
                    reader.GetFieldValue<bool>(os.IsEmailVerified))
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyOtpCellPhone failed for CellPhoneNumber {CellPhoneNumber}", cellPhoneNumber);
            throw;
        }
    }

    public async Task<List<SourceMachineRegistrations>> GetByIdsAsync(IEnumerable<int> sourceMachineIds, int maxDegreeOfParallelism)
    {
        var results = new ConcurrentBag<SourceMachineRegistrations>();

        await Parallel.ForEachAsync(
            sourceMachineIds,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            async (sourceMachineId, _) =>
            {
                var registration = await GetByIdPreferringScyllaAsync(sourceMachineId);
                if (registration is not null)
                    results.Add(registration);
            });

        return [.. results];
    }

    /// <summary>
    /// Looks up a single device's registration by SourceMachineId, preferring the existing
    /// "registrations" Scylla table and falling back to PostgreSQL when Scylla has no row yet (CDC
    /// lag) or is unreachable. A Scylla failure here is deliberately not rethrown -- PostgreSQL is
    /// the durable source of truth, so a temporarily unavailable Scylla cluster should degrade this
    /// to a normal PostgreSQL read rather than fail it.
    /// </summary>
    private async Task<SourceMachineRegistrations?> GetByIdPreferringScyllaAsync(int sourceMachineId)
    {
        var log = _logger.WithCaller();

        try
        {
            var fromScylla = await _cqlExecutor.QuerySingleAsync(
                QueryRegistrations.GetBySourceMachineIdCql,
                p => p.AddWithValue(pn.SourceMachineId, sourceMachineId),
                row => row.ToSourceMachineRegistration());

            if (fromScylla is not null)
                return fromScylla;
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable fetching SourceMachineId {SourceMachineId}; falling back to Postgres", sourceMachineId);
            await TryHealScyllaSessionAsync(_logger, nameof(GetByIdPreferringScyllaAsync));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Scylla lookup failed for SourceMachineId {SourceMachineId}; falling back to Postgres", sourceMachineId);
        }

        return await _sqlExecutor.QuerySingleAsync
        (
            QueryRegistrations.GetBySourceMachineIdSql,
            p => p.AddWithValue(pn.SourceMachineId, sourceMachineId),
            reader => reader.ToSourceMachineRegistration()
        );
    }

    public async Task SetOwningPersonIfUnsetAsync(int sourceMachineId, int personId)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync
            (
                QueryRegistrations.SetOwningPersonIfUnsetSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OwningPersonId, personId);
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetOwningPersonIfUnsetAsync failed for SourceMachineId {SourceMachineId}, PersonId {PersonId}", sourceMachineId, personId);
            throw;
        }
    }

    public async Task SetKeyDeliveryMethodAsync(int sourceMachineId, KeyDeliveryMethods keyDeliveryMethod)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync
            (
                QueryRegistrations.SetKeyDeliveryMethodSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.KeyDeliveryMethod, (int)keyDeliveryMethod);
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetKeyDeliveryMethodAsync failed for SourceMachineId {SourceMachineId}, KeyDeliveryMethod {KeyDeliveryMethod}", sourceMachineId, keyDeliveryMethod);
            throw;
        }
    }

    public async Task SetGroupShellIdIfUnsetAsync(int sourceMachineId, int groupShellId)
    {
        try
        {
            await _sqlExecutor.ExecuteAsync
            (
                QueryRegistrations.SetGroupShellIdIfUnsetSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.GroupShellId, groupShellId);
                }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetGroupShellIdIfUnsetAsync failed for SourceMachineId {SourceMachineId}, GroupShellId {GroupShellId}", sourceMachineId, groupShellId);
            throw;
        }
    }

}
