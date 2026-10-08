using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IPersonMemberAddStrikeRepository"/>
/// <remarks>
/// A miss is read, decided by <see cref="MemberAddStrikePolicy"/>, and written compare-and-set: the
/// write lands only if the row is still what the policy was given, and a lost race reads again.
/// Writes go to PostgreSQL only; Scylla's copy follows through CDC
/// (<see cref="Cdc.PersonMemberAddStrikesCdcSyncHandler"/>).
/// </remarks>
public class PersonMemberAddStrikeRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<PersonMemberAddStrikeRepository> logger)
    : IPersonMemberAddStrikeRepository
{
    /// <summary>
    /// How many read-decide-write rounds a miss gets. Each lost round means another miss by the same
    /// admin landed in that instant; more than a few in a row is not contention but a fault.
    /// </summary>
    internal const int RecordMissAttempts = 5;

    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<PersonMemberAddStrikeRepository> _logger = logger.Initializer();

    public async Task<PersonMemberAddStrikes?> GetAsync(int personId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonMemberAddStrikes.GetByPersonIdSql,
                p => p.AddWithValue(pn.PersonId, personId),
                reader => reader.ToPersonMemberAddStrikes()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task<PersonMemberAddStrikes> RecordMissAsync(int personId, TimeSpan baseWindow)
    {
        if (baseWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseWindow), baseWindow, "The strike window must be positive.");

        try
        {
            for (var attempt = 0; attempt < RecordMissAttempts; attempt++)
            {
                var current = await GetAsync(personId);
                var now = DateTimeOffset.UtcNow;
                var next = MemberAddStrikePolicy.AfterMiss(current, personId, now, baseWindow);

                var stored = current is null
                    ? await _sqlExecutor.QuerySingleAsync
                    (
                        QueryPersonMemberAddStrikes.InsertIfAbsentSql,
                        p =>
                        {
                            p.AddWithValue(pn.PersonId, personId);
                            p.AddWithValue(pn.StrikeCount, next.StrikeCount);
                            p.AddWithValue(pn.WindowEndsOn, (object?)next.WindowEndsOn ?? DBNull.Value);
                            p.AddWithValue(pn.LockedOn, (object?)next.LockedOn ?? DBNull.Value);
                            p.AddWithValue(pn.Now, now);
                        },
                        reader => reader.ToPersonMemberAddStrikes()
                    )
                    : await _sqlExecutor.QuerySingleAsync
                    (
                        QueryPersonMemberAddStrikes.UpdateIfUnchangedSql,
                        p =>
                        {
                            p.AddWithValue(pn.PersonId, personId);
                            p.AddWithValue(pn.StrikeCount, next.StrikeCount);
                            p.AddWithValue(pn.WindowEndsOn, (object?)next.WindowEndsOn ?? DBNull.Value);
                            p.AddWithValue(pn.LockedOn, (object?)next.LockedOn ?? DBNull.Value);
                            p.AddWithValue(pn.Now, now);
                            p.AddWithValue(pn.ExpectedStrikeCount, current.StrikeCount);
                            p.Add(new Npgsql.NpgsqlParameter(pn.ExpectedWindowEndsOn, NpgsqlTypes.NpgsqlDbType.TimestampTz)
                            {
                                Value = (object?)current.WindowEndsOn ?? DBNull.Value
                            });
                        },
                        reader => reader.ToPersonMemberAddStrikes()
                    );

                if (stored is not null)
                {
                    if (stored.IsLocked && current?.IsLocked != true)
                        _logger.LogWarning("Member-add strikes locked PersonId: [{PersonId}] at strike [{StrikeCount}]", personId, stored.StrikeCount);

                    return stored;
                }
            }

            throw new InvalidOperationException(
                $"Recording a member-add miss for PersonId {personId} lost the race {RecordMissAttempts} times in a row.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RecordMissAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task<PersonMemberAddStrikes?> ClearAsync(int personId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryPersonMemberAddStrikes.ClearSql,
                p =>
                {
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToPersonMemberAddStrikes()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClearAsync failed for PersonId: [{PersonId}]", personId);
            throw;
        }
    }

    public async Task<int> ResetEndedWindowsAsync()
    {
        try
        {
            return await _sqlExecutor.ExecuteAsync
            (
                QueryPersonMemberAddStrikes.ResetEndedWindowsSql,
                p => p.AddWithValue(pn.Now, DateTimeOffset.UtcNow)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ResetEndedWindowsAsync failed");
            throw;
        }
    }
}
