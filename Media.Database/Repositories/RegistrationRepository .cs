using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Helpers;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
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
/// PostgreSQL-backed implementation of <see cref="IRegistrationRepository"/>. Writes go to PostgreSQL
/// synchronously within a transaction; the equivalent Scylla/Cassandra rows are then updated
/// on a background task queue, with self-healing if the Scylla session becomes unreachable.
/// </summary>
/// <param name="sqlExecutor">The SQL query executor.</param>
/// <param name="cqlExecutor">The CQL query executor.</param>
/// <param name="scyllaProvider">The Scylla session provider.</param>
/// <param name="unitOfWorkFactory">The factory function to create a unit of work.</param>
/// <param name="backgroundTaskQueue">The background task queue.</param>
/// <param name="registrationSettings">The registration/OTP workflow settings.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="levelSwitch">The logging level switch.</param>
public class RegistrationRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    Func<IUnitOfWork> unitOfWorkFactory,
    IBackgroundTaskQueue backgroundTaskQueue,
    IOptions<RegistrationSettings> registrationSettings,
    ILogger<RegistrationRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(scyllaProvider), IRegistrationRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue = backgroundTaskQueue;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly IOptions<RegistrationSettings> _registrationSettings = registrationSettings;
    private readonly FluentLogger<RegistrationRepository> _logger = logger.Initializer();

    private readonly LoggingLevelSwitch _levelswitch = levelSwitch;
    private readonly int _scyllaMaxBatchsize = scyllaProvider.MaxBatchSize;

    public async Task<SourceMachineRegistrations?> AddBySourceInformation(AddSourceInformationRequest request)
    {
        try
        {
            var addSourceResponse = await _sqlExecutor.QuerySingleAsync
            (
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
                return null;

            var otpEmail = OneTimePassword.Generate();
            var otpCellPhone = OneTimePassword.Generate();

            var addRegistrationResponse = await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.AddRegistrationBySourceMachineUuidSql,
                p => 
                {
                    p.AddWithValue(pn.SourceMachineUuid, addSourceResponse.SourceMachineUuid);
                    p.AddWithValue(pn.OtpEmail, otpEmail);
                    p.AddWithValue(pn.OtpCellPhone, otpCellPhone);
                },
                reader => reader.ToSourceMachineRegistration()
            );

            ///TODO: Add background process to send OTP to email and cell phone number asynchronously

            return addRegistrationResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddBySourceInformation failed for SourceMachineName {SourceMachineName}", request.SourceMachineName);
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
        try
        {
            var existingRegistration = await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.GetBySourceMachineUuidSql,
                p => p.AddWithValue(pn.SourceMachineUuid, request.SourceMachineUuid),
                reader => reader.ToSourceMachineRegistration()
            );

            if (existingRegistration is null) 
                return null;

            var updateResponse = await _sqlExecutor.QuerySingleAsync
            (
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
                return null;

            if (existingRegistration.EmailAddress == request.EmailAddress
                && existingRegistration.CellPhoneNumber == request.CellPhoneNumber)
            {
                QueueBackgroundUpdateAsync(updateResponse);
                return updateResponse;
            }

            var ids = await _sqlExecutor.QueryManyAsync
            (
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
                return null;

            updateResponse.OtpEmail = addRegistrationResponse.OtpEmail;
            updateResponse.OtpCellPhone = addRegistrationResponse.OtpCellPhone;
            updateResponse.RegistrationId = addRegistrationResponse.Id;
            updateResponse.RegistrationInsertedOn = addRegistrationResponse.InsertedOn;
            updateResponse.RegistrationUpdatedOn = addRegistrationResponse.UpdatedOn;

            QueueBackgroundUpdateAsync(updateResponse);
            return updateResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSourceInformation failed for SourceMachineUuid {SourceMachineUuid}", request.SourceMachineUuid);
            throw;
        }
    }

    public async Task<ResendOtpResult?> ResendOtp(Guid sourceMachineUuid)
    {
        try
        {
            var existingRegistration = await _sqlExecutor.QuerySingleAsync
            (
                QueryRegistrations.GetBySourceMachineUuidSql,
                p => p.AddWithValue(pn.SourceMachineUuid, sourceMachineUuid),
                reader => reader.ToSourceMachineRegistration()
            );

            if (existingRegistration is null)
                return null;

            if (existingRegistration.IsEmailVerified && existingRegistration.IsSmsVerified)
            {
                return new ResendOtpResult
                {
                    EmailOtpSent = false,
                    SmsOtpSent = false
                };
            }

            await _sqlExecutor.QueryManyAsync
            (
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
                return null;

            ///TODO: Add background process to send OTP to email and cell phone number asynchronously

            return new ResendOtpResult
            {
                EmailOtpSent = !existingRegistration.IsEmailVerified,
                SmsOtpSent = !existingRegistration.IsSmsVerified
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResendOtp failed for SourceMachineUuid {SourceMachineUuid}", sourceMachineUuid);
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

    /// <summary>
    /// Queues a background task to upsert a registration in the Scylla database asynchronously.
    /// </summary>
    /// <param name="registration">The registration to upsert.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task QueueBackgroundUpdateAsync(SourceMachineRegistrations registration)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await UpsertCqlAsync(registration);
        });
    }

    private async Task UpsertCqlAsync(SourceMachineRegistrations registration)
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

            log.LogInformation("Background CQL completed for RegistrationId {Id}", registration.RegistrationId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for RegistrationId {Id}", registration.RegistrationId);
            await TryHealScyllaSessionAsync(_logger, nameof(UpsertCqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundUpdate task failed for RegistrationId {Id}", registration.RegistrationId);
            throw;
        }
    }

    /// <summary>
    /// Queues a background task to update the metadata of a file in the Scylla database asynchronously.
    /// </summary>
    /// <param name="file">The file whose metadata is to be updated.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task QueueBackgroundUpdateMetadataAsync(Files file)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await UpdateMetadataCqlAsync(file);
        });
    }

    /// <summary>
    /// Updates the metadata of a file in the Scylla database asynchronously.       
    /// </summary>
    /// <param name="file"></param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task UpdateMetadataCqlAsync(Files file)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryFiles.UpdateCql, p =>
            {
                p.AddWithValue(pn.Id, file.Id);
                p.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
                p.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
                p.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            });

            log.LogInformation("Background CQL metadata update completed for FileId {Id}", file.Id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", file.Id);
            await TryHealScyllaSessionAsync(_logger, nameof(UpdateMetadataCqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundUpdate task failed for FileId {Id}", file.Id);
            throw;
        }
    }

    /// <summary>
    /// Deletes a file from the SQL database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation, containing the deleted file if found.</returns>
    public async Task<Files?> Delete(Guid id)
    {
        try
        {
            var file = await _sqlExecutor.QuerySingleAsync(
                QueryFiles.DeleteSql,
                p => p.AddWithValue(pn.Id, id),
                reader => reader.ToFile());

            if (file != null)
                _ = QueueBackgroundDeleteAsync(id);

            return file;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete failed for FileId {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Deletes the history of files by source machine ID and original file path from the Scylla database asynchronously.
    /// </summary>
    /// <param name="sourceMachineId">The ID of the source machine.</param>
    /// <param name="originalFilePath">The original file path.</param>
    /// <returns>A task representing the asynchronous operation, containing the list of deleted files.</returns>
    public async Task<List<Files>> DeleteHistoryBySourceMachineId(int sourceMachineId, string originalFilePath)
    {
        try
        {
            var files = await _sqlExecutor.QueryManyAsync(
                QueryFiles.DeleteHistorySql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.OriginalFilePath, originalFilePath);
                },
                reader => reader.ToFile());

            if (files.Count > 0)
                _ = QueueBackgroundDeleteAsync(files);

            return files;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeleteHistoryBySourceMachineId failed for SourceMachineId {SourceMachineId}, OriginalFilePath {OriginalFilePath}",
                sourceMachineId, originalFilePath);
            throw;
        }
    }

    /// <summary>
    /// Queues a background task to delete a file from the Scylla database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task QueueBackgroundDeleteAsync(Guid id)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await DeleteCqlAsync(id);
        });
    }

    /// <summary>
    /// Deletes a file from the Scylla database asynchronously.
    /// </summary>
    /// <param name="id">The ID of the file to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteCqlAsync(Guid id)
    {
        var log = _logger.WithCaller();

        try
        {
            await _cqlExecutor.ExecuteAsync(QueryFiles.DeleteCql, p => p.AddWithValue(pn.Id, id));

            log.LogInformation("Background CQL delete completed for FileId {Id}", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", id);
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteCqlAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundDelete task failed for FileId {Id}", id);
            throw;
        }
    }

    /// <summary>
    /// Queues a background task to delete a batch of files from the Scylla database asynchronously.
    /// </summary>
    /// <param name="files">The list of files to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>

    private async Task QueueBackgroundDeleteAsync(List<Files> files)
    {
        await _backgroundTaskQueue.QueueBackgroundWorkItemAsync(async cancellationToken =>
        {
            await DeleteCqlBatchAsync(files);
        });
    }

    /// <summary>
    /// Deletes a batch of files from the Scylla database asynchronously.
    /// </summary>
    /// <param name="files">The list of files to delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task DeleteCqlBatchAsync(List<Files> files)
    {
        var log = _logger.WithCaller();

        try
        {
            var cqlConnection = GetCqlConnection();

            CqlCommand cqlCommand = cqlConnection.GetCqlCommand(
                QueryFiles.DeleteCql, _scyllaMaxBatchsize)!;

            cqlCommand.BeginBatch();

            var tasks = files.Select(file => cqlCommand.AddQuery(file.Id));

            await Task.WhenAll(tasks);
            await cqlCommand.EndBatch();

            log.LogInformation("Background CQL batch delete completed for {Count} files", files.Count);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable during batch delete");
            await TryHealScyllaSessionAsync(_logger, nameof(DeleteCqlBatchAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundDelete batch task failed");
            throw;
        }
    }

}
