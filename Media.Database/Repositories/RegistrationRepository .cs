using Cassandra;
using Media.Common.BackgroundJobs;
using Media.Common.Helpers;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
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
/// <param name="scyllaProvider">The Scylla session provider.</param>
/// <param name="unitOfWorkFactory">The factory function to create a unit of work.</param>
/// <param name="backgroundTaskQueue">The background task queue.</param>
/// <param name="logger">The logger instance.</param>
/// <param name="levelSwitch">The logging level switch.</param>
public class RegistrationRepository(
    ISqlQueryExecutor sqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    Func<IUnitOfWork> unitOfWorkFactory,
    IBackgroundTaskQueue backgroundTaskQueue,
    ILogger<RegistrationRepository> logger,
    LoggingLevelSwitch levelSwitch)
    : BaseRepository(scyllaProvider), IRegistrationRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IBackgroundTaskQueue _backgroundTaskQueue = backgroundTaskQueue;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
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
                reader => reader.ToSourceMachineRegistration()
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
                p => p.AddWithValue(pn.SourceMachineUuid, request.SourceMachineUuid),
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

            updateResponse.OtpEmail = addRegistrationResponse?.Result?.OtpEmail!;
            updateResponse.OtpCellPhone = addRegistrationResponse?.Result?.OtpCellPhone!;
            updateResponse.RegistrationId = (int)addRegistrationResponse?.Result?.Id!;
            updateResponse.RegistrationInsertedOn = addRegistrationResponse?.Result?.InsertedOn;
            updateResponse.RegistrationUpdatedOn = addRegistrationResponse?.Result?.UpdatedOn;


            QueueBackgroundUpdateAsync(updateResponse);
            return updateResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateSourceInformation failed for SourceMachineUuid {SourceMachineUuid}", request.SourceMachineUuid);
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
            var cqlConnection = GetCqlConnection();
            var cqlCommand = cqlConnection.GetCqlCommand(QueryRegistrations.UpsertRegistrationCql);
            cqlCommand.Parameters.AddWithValue(pn.RegistrationId, registration.RegistrationId);
            cqlCommand.Parameters.AddWithValue(pn.SourceMachineId, registration.SourceMachineId);
            cqlCommand.Parameters.AddWithValue(pn.SourceMachineUuid, registration.SourceMachineUuid);
            cqlCommand.Parameters.AddWithValue(pn.SourceMachineName, registration.SourceMachineName);
            cqlCommand.Parameters.AddWithValue(pn.DeviceTypeId, (int)registration.DeviceTypeId);
            cqlCommand.Parameters.AddWithValue(pn.FirstName, registration.FirstName);
            cqlCommand.Parameters.AddWithValue(pn.LastName, registration.LastName);
            cqlCommand.Parameters.AddWithValue(pn.EmailAddress, registration.EmailAddress);
            cqlCommand.Parameters.AddWithValue(pn.CellPhoneNumber, registration.CellPhoneNumber);
            cqlCommand.Parameters.AddWithValue(pn.OperatingSystem, registration.OperatingSystem);
            cqlCommand.Parameters.AddWithValue(pn.SourceInsertedOn, registration.InsertedOn);
            cqlCommand.Parameters.AddWithValue(pn.SourceUpdatedOn, registration.UpdatedOn!);
            cqlCommand.Parameters.AddWithValue(pn.IsActive, registration.IsActive);
            cqlCommand.Parameters.AddWithValue(pn.OtpEmail, registration.OtpEmail);
            cqlCommand.Parameters.AddWithValue(pn.OtpCellPhone, registration.OtpCellPhone);
            cqlCommand.Parameters.AddWithValue(pn.IsEmailVerified, registration.IsEmailVerified);
            cqlCommand.Parameters.AddWithValue(pn.IsSmsVerified, registration.IsSmsVerified);
            cqlCommand.Parameters.AddWithValue(pn.RegistrationInsertedOn, registration.RegistrationInsertedOn!);
            cqlCommand.Parameters.AddWithValue(pn.RegistrationUpdatedOn, registration.RegistrationUpdatedOn!);
            await cqlCommand.ExecuteRowSet();

            log.LogInformation("Background CQL completed for RegistrationId {Id}", registration.RegistrationId);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for RegistrationId {Id}", registration.RegistrationId);
            await TryHealScyllaSessionAsync(nameof(UpsertCqlAsync));
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
            var cqlConnection = GetCqlConnection();
            var cqlCommand = cqlConnection.GetCqlCommand(QueryFiles.UpdateCql);

            cqlCommand.Parameters.AddWithValue(pn.Id, file.Id);
            cqlCommand.Parameters.AddWithValue(pn.UpdatedOn, file.UpdatedOn!);
            cqlCommand.Parameters.AddWithValue(pn.LastFileUpdate, file.LastFileUpdate!);
            cqlCommand.Parameters.AddWithValue(pn.Metadata, file.Metadata!.ToJsonString());
            await cqlCommand.ExecuteRowSet();

            log.LogInformation("Background NoSQL metadata update completed for FileId {Id}", file.Id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", file.Id);
            await TryHealScyllaSessionAsync(nameof(UpdateMetadataCqlAsync));
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
            var cqlConnection = GetCqlConnection();
            var cqlCommand = cqlConnection.GetCqlCommand(QueryFiles.DeleteCql);

            cqlCommand.Parameters.AddWithValue(pn.Id, id);
            await cqlCommand.ExecuteRowSet();

            log.LogInformation("Background NoSQL delete completed for FileId {Id}", id);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable for FileId {Id}", id);
            await TryHealScyllaSessionAsync(nameof(DeleteCqlAsync));
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

            log.LogInformation("Background NoSQL batch delete completed for {Count} files", files.Count);
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable during batch delete");
            await TryHealScyllaSessionAsync(nameof(DeleteCqlBatchAsync));
            throw;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "BackgroundDelete batch task failed");
            throw;
        }
    }

    /// <summary>
    /// Cassandra driver exceptions that indicate the cluster/session is unreachable, as opposed to a single
    /// query timing out against an otherwise healthy cluster. Only these warrant rebuilding the session.
    /// </summary>
    /// <param name="ex">The exception to check.</param>
    /// <returns>True if the exception indicates a connectivity issue with the Scylla cluster; otherwise, false.</returns>
    private static bool IsScyllaConnectivityException(Exception ex) =>
        ex is NoHostAvailableException or UnavailableException or OperationTimedOutException;

    /// <summary>
    /// Attempts to heal the Scylla session without letting a healing failure (e.g. a busy self-heal lock)
    /// mask the original exception that triggered the heal attempt.
    /// </summary>
    /// <param name="methodName">The name of the method that triggered the heal attempt.</param>    
    private async Task TryHealScyllaSessionAsync(string methodName)
    {
        try
        {
            await ScyllaProvider!.HealSessionAsync(ScyllaProvider.GetCurrentSessionId(), methodName);
        }
        catch (Exception healEx)
        {
            _logger.WithCaller().LogError(healEx, "Scylla session heal attempt failed in {OriginatingMethod}", methodName);
        }
    }
}
