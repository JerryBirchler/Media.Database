using Media.Common.Helpers;
using Media.Common.Helpers.Fluent;
using Media.Common.Settings;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Core;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of IRegistrationRepository. Writes go to PostgreSQL
/// synchronously; Scylla is kept in sync separately and asynchronously by the CDC pipeline
/// (Media.Common.Cdc.CdcConsumerService dispatching to Cdc.RegistrationsCdcSyncHandler),
/// reading Postgres own write-ahead log rather than this repository writing to both stores.
/// </summary>
public class RegistrationRepository(
    ISqlQueryExecutor sqlExecutor,
    Func<IUnitOfWork> unitOfWorkFactory,
    IOptions<RegistrationSettings> registrationSettings,
    ILogger<RegistrationRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : IRegistrationRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly IOptions<RegistrationSettings> _registrationSettings = registrationSettings;
    private readonly FluentLogger<RegistrationRepository> _logger = logger.Initializer();

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
                    p.AddWithValue(pn.DeviceTypeId, request.DeviceTypeId);
                    p.AddWithValue(pn.EmailAddress, request.EmailAddress);
                    p.AddWithValue(pn.CellPhoneNumber, request.CellPhoneNumber);
                    p.AddWithValue(pn.FirstName, request.FirstName);
                    p.AddWithValue(pn.LastName, request.LastName);
                    p.AddWithValue(pn.OperatingSystem, request.OperatingSystem);
                },
                reader => reader.ToSourceMachineRegistration()
            );

            if (addSourceResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            var otpEmail = OneTimePassword.Generate();
            var otpCellPhone = OneTimePassword.Generate();

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
                reader => reader.ToSourceMachineRegistration()
            );

            if (addRegistrationResponse is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            addSourceResponse.OtpEmail = addRegistrationResponse.OtpEmail;
            addSourceResponse.OtpCellPhone = addRegistrationResponse.OtpCellPhone;
            addSourceResponse.RegistrationId = addRegistrationResponse.RegistrationId;
            addSourceResponse.RegistrationInsertedOn = addRegistrationResponse.RegistrationInsertedOn;
            addSourceResponse.RegistrationUpdatedOn = addRegistrationResponse.RegistrationUpdatedOn;

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

    public async Task<SourceMachineRegistrations?> UpdateSourceInformation(UpdateSourceInformationRequest request)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var existingRegistration = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.GetBySourceMachineUuidSql,
                p => p.AddWithValue(pn.SourceMachineUuid, request.SourceMachineUuid),
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
                    p.AddWithValue(pn.SourceMachineUuid, request.SourceMachineUuid);
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
                    p.AddWithValue(pn.SourceMachineUuid, request.SourceMachineUuid);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToRegistrationIds()
            );

            var otpEmail = existingRegistration.IsEmailVerified ? string.Empty : OneTimePassword.Generate();
            var otpCellPhone = existingRegistration.IsSmsVerified ? string.Empty : OneTimePassword.Generate();

            var addRegistrationResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.AddRegistrationBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, request.SourceMachineUuid);
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
            _logger.LogError(ex, "UpdateSourceInformation failed for SourceMachineUuid {SourceMachineUuid}", request.SourceMachineUuid);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<ResendOtpResult?> ResendOtp(Guid sourceMachineUuid)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var existingRegistration = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.GetBySourceMachineUuidSql,
                p => p.AddWithValue(pn.SourceMachineUuid, sourceMachineUuid),
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
                    SmsOtpSent = false
                };
            }

            await _sqlExecutor.QueryManyAsync
            (
                uow,
                QueryRegistrations.InactivateRegistrationsBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, sourceMachineUuid);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToRegistrationIds()
            );

            var otpEmail = existingRegistration.IsEmailVerified ? string.Empty : OneTimePassword.Generate();
            var otpCellPhone = existingRegistration.IsSmsVerified ? string.Empty : OneTimePassword.Generate();

            var addRegistrationResponse = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryRegistrations.AddRegistrationBySourceMachineUuidSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, sourceMachineUuid);
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

            ///TODO: Add background process to send OTP to email and cell phone number asynchronously

            await uow.CommitAsync();

            return new ResendOtpResult
            {
                EmailOtpSent = !existingRegistration.IsEmailVerified,
                SmsOtpSent = !existingRegistration.IsSmsVerified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResendOtp failed for SourceMachineUuid {SourceMachineUuid}", sourceMachineUuid);

            if (uow.CurrentTransaction != null)
                await uow.RollbackAsync();

            throw;
        }
    }

    public async Task<OtpEmailResponse?> VerifyOtpEmail(Guid sourceMachineUuid, string otp)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.VerifyOtpEmailSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, sourceMachineUuid);
                    p.AddWithValue(pn.OtpEmail, otp);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                    p.AddWithValue(pn.OtpWindowStart, DateTimeOffset.UtcNow - _registrationSettings.Value.OtpWindow);
                },
                reader => reader.ToOtpEmailResponse()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyOtpEmail failed for SourceMachineUuid {SourceMachineUuid}", sourceMachineUuid);
            throw;
        }
    }

    public async Task<OtpSmsResponse?> VerifyOtpCellPhone(Guid sourceMachineUuid, string otp)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.VerifyOtpCellPhoneSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineUuid, sourceMachineUuid);
                    p.AddWithValue(pn.OtpCellPhone, otp);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                    p.AddWithValue(pn.OtpWindowStart, DateTimeOffset.UtcNow - _registrationSettings.Value.OtpWindow);
                },
                reader => reader.ToOtpSmsResponse()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "VerifyOtpCellPhone failed for SourceMachineUuid {SourceMachineUuid}", sourceMachineUuid);
            throw;
        }
    }
}
