using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IPersonVoiceProfileRepository"/>
public class PersonVoiceProfileRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<PersonVoiceProfileRepository> logger)
    : IPersonVoiceProfileRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<PersonVoiceProfileRepository> _logger = logger.Initializer();

    public async Task<PersonVoiceProfile?> AddAsync(
        int personId, SpeakerRecognitionProviders provider, string profileData, DateTimeOffset consentedOn)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonVoiceProfiles.AddSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.Provider, (int)provider);
                    p.AddWithValue(pn.ProfileData, profileData);
                    p.AddWithValue(pn.ConsentedOn, consentedOn);
                },
                reader => reader.ToPersonVoiceProfile()
            );
        }
        catch (Exception ex)
        {
            // The profile data is never logged, here or anywhere: it is the biometric artifact.
            _logger.LogError(ex, "AddAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task<PersonVoiceProfile?> GetActiveByPersonIdAsync(int personId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonVoiceProfiles.GetActiveByPersonIdSql,
                p => p.AddWithValue(pn.PersonId, personId),
                reader => reader.ToPersonVoiceProfile()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveByPersonIdAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task<List<PersonVoiceProfile>> GetActiveByPersonIdsAsync(IEnumerable<int> personIds)
    {
        var ids = personIds as int[] ?? [.. personIds];

        if (ids.Length == 0)
            return [];

        try
        {
            // One statement with an array parameter rather than a query per person: the caller is
            // resolving a single utterance against a whole candidate set, and that set is already
            // bounded by the group.
            return await _sqlExecutor.QueryManyAsync
            (
                QueryPersonVoiceProfiles.GetActiveByPersonIdsSql,
                p => p.AddWithValue(pn.PersonId, ids),
                reader => reader.ToPersonVoiceProfile()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveByPersonIdsAsync failed for PersonCount: [{PersonCount}]", ids.Length);
            throw;
        }
    }

    public async Task<PersonVoiceProfile?> RevokeByPersonIdAsync(int personId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonVoiceProfiles.RevokeByPersonIdSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.RevokedOn, DateTimeOffset.UtcNow);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToPersonVoiceProfile()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RevokeByPersonIdAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }
}
