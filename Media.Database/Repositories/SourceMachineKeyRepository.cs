using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="ISourceMachineKeyRepository"/>
public class SourceMachineKeyRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<SourceMachineKeyRepository> logger)
    : ISourceMachineKeyRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<SourceMachineKeyRepository> _logger = logger.Initializer();

    public async Task<SourceMachineKey> EnrollAsync(int sourceMachineId, SourceMachineKeyPurpose keyPurpose, SourceMachineKeyAlgorithm algorithm, string publicKey)
    {
        try
        {
            var result = await _sqlExecutor.QuerySingleAsync(
                QuerySourceMachineKeys.EnrollSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.KeyPurpose, (int)keyPurpose);
                    p.AddWithValue(pn.Algorithm, (int)algorithm);
                    p.AddWithValue(pn.PublicKey, publicKey);
                },
                reader => reader.ToSourceMachineKey());

            // EnrollSql's RETURNING clause always produces exactly one row.
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EnrollAsync failed for SourceMachineId {SourceMachineId}, KeyPurpose {KeyPurpose}", sourceMachineId, keyPurpose);
            throw;
        }
    }

    public async Task<List<SourceMachineKey>> GetActiveBySourceMachineIdAsync(int sourceMachineId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync(
                QuerySourceMachineKeys.GetActiveBySourceMachineIdSql,
                p => p.AddWithValue(pn.SourceMachineId, sourceMachineId),
                reader => reader.ToSourceMachineKey());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveBySourceMachineIdAsync failed for SourceMachineId {SourceMachineId}", sourceMachineId);
            throw;
        }
    }

    public async Task<SourceMachineKey?> GetActiveByPublicKeyAsync(string publicKey)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QuerySourceMachineKeys.GetActiveByPublicKeySql,
                p => p.AddWithValue(pn.PublicKey, publicKey),
                reader => reader.ToSourceMachineKey());
        }
        catch (Exception ex)
        {
            // Deliberately does not log the key itself.
            _logger.LogError(ex, "GetActiveByPublicKeyAsync failed");
            throw;
        }
    }

    public async Task<SourceMachineKey?> RevokeIfActiveAsync(Guid sourceMachineKeyUuid)
    {
        try
        {
            var revokedOn = DateTimeOffset.UtcNow;

            return await _sqlExecutor.QuerySingleAsync(
                QuerySourceMachineKeys.RevokeIfActiveSql,
                p =>
                {
                    p.AddWithValue(pn.SourceMachineKeyUuid, sourceMachineKeyUuid);
                    p.AddWithValue(pn.RevokedOn, revokedOn);
                    p.AddWithValue(pn.UpdatedOn, revokedOn);
                },
                reader => reader.ToSourceMachineKey());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RevokeIfActiveAsync failed for SourceMachineKeyUuid {SourceMachineKeyUuid}", sourceMachineKeyUuid);
            throw;
        }
    }
}
