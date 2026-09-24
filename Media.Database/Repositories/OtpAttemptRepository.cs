using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IOtpAttemptRepository"/>
public class OtpAttemptRepository(
    ICqlQueryExecutor cqlExecutor,
    ILogger<OtpAttemptRepository> logger) : IOtpAttemptRepository
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly FluentLogger<OtpAttemptRepository> _logger = logger.Initializer();

    public async Task<OtpAttempt?> GetAsync(string nonceId)
    {
        try
        {
            return await _cqlExecutor.QuerySingleAsync(
                QueryOtpAttempts.GetSql,
                p => p.AddWithValue(pn.NonceId, nonceId),
                row => row.ToOtpAttempt());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetAsync failed for an OTP attempt counter");
            throw;
        }
    }

    public async Task SaveAsync(OtpAttempt attempt, TimeSpan timeToLive)
    {
        // Scylla rejects a TTL of zero or less as "no expiry" or an error; a counter must always
        // expire, so anything shorter than a second is clamped up to one.
        var seconds = Math.Max(1, (int)Math.Ceiling(timeToLive.TotalSeconds));

        try
        {
            await _cqlExecutor.ExecuteAsync(
                QueryOtpAttempts.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.NonceId, attempt.NonceId);
                    p.AddWithValue(pn.Attempts, attempt.Attempts);
                    p.AddWithValue(pn.IsUsed, attempt.IsUsed);
                    p.AddWithValue(pn.IssuedOn, attempt.IssuedOn);
                    p.AddWithValue(pn.TimeToLiveSeconds, seconds);
                });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "SaveAsync failed for an OTP attempt counter");
            throw;
        }
    }
}
