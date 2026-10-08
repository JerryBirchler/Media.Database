using Media.Common.Archetypes;
using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupInviteRepository"/>
/// <remarks>
/// Writes go to PostgreSQL only; Scylla's copy follows through CDC
/// (<see cref="Cdc.GroupInvitesCdcSyncHandler"/>). Addresses and names are never logged.
/// </remarks>
public class GroupInviteRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<GroupInviteRepository> logger)
    : IGroupInviteRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<GroupInviteRepository> _logger = logger.Initializer();

    public async Task<GroupInvite> CreateAsync(int groupId, int invitedByPersonId, EmailAddress emailAddress, PersonName lastName, TimeSpan lifetime)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "An invite's lifetime must be positive.");

        var now = DateTimeOffset.UtcNow;

        try
        {
            var invite = await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupInvites.CreateSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.InvitedByPersonId, invitedByPersonId);
                    p.AddWithValue(pn.EmailAddress, emailAddress.ToString());
                    p.AddWithValue(pn.LastName, lastName.ToString());
                    p.AddWithValue(pn.ExpiresOn, now + lifetime);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.ToGroupInvite()
            );

            // An insert or an upsert always returns its row; nothing back means the statement did not run.
            return invite ?? throw new InvalidOperationException($"Creating an invite to GroupId {groupId} returned no row.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed for GroupId: [{GroupId}], InvitedByPersonId: [{InvitedByPersonId}]", groupId, invitedByPersonId);
            throw;
        }
    }

    public async Task<GroupInvite?> GetByUuidAsync(Guid groupInviteUuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupInvites.GetByUuidSql,
                p => p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid),
                reader => reader.ToGroupInvite()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByUuidAsync failed for GroupInviteUuid: [{GroupInviteUuid}]", groupInviteUuid);
            throw;
        }
    }

    public async Task<List<GroupInvite>> ListPendingByGroupAsync(int groupId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryGroupInvites.ListPendingByGroupSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupInvite()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListPendingByGroupAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<List<ReceivedGroupInvite>> ListPendingForEmailAsync(EmailAddress emailAddress)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryGroupInvites.ListPendingForEmailSql,
                p =>
                {
                    p.AddWithValue(pn.EmailAddress, emailAddress.ToString());
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToReceivedGroupInvite()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListPendingForEmailAsync failed");
            throw;
        }
    }

    public async Task<GroupInvite?> AcceptAsync(Guid groupInviteUuid, EmailAddress recipientEmailAddress, int acceptedByPersonId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupInvites.AcceptSql,
                p =>
                {
                    p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid);
                    p.AddWithValue(pn.EmailAddress, recipientEmailAddress.ToString());
                    p.AddWithValue(pn.AcceptedByPersonId, acceptedByPersonId);
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupInvite()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AcceptAsync failed for GroupInviteUuid: [{GroupInviteUuid}], AcceptedByPersonId: [{AcceptedByPersonId}]", groupInviteUuid, acceptedByPersonId);
            throw;
        }
    }

    public async Task<GroupInvite?> DeclineAsync(Guid groupInviteUuid, EmailAddress recipientEmailAddress)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupInvites.DeclineSql,
                p =>
                {
                    p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid);
                    p.AddWithValue(pn.EmailAddress, recipientEmailAddress.ToString());
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupInvite()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeclineAsync failed for GroupInviteUuid: [{GroupInviteUuid}]", groupInviteUuid);
            throw;
        }
    }

    public async Task<GroupInvite?> CancelAsync(int groupId, Guid groupInviteUuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupInvites.CancelSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid);
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupInvite()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CancelAsync failed for GroupId: [{GroupId}], GroupInviteUuid: [{GroupInviteUuid}]", groupId, groupInviteUuid);
            throw;
        }
    }

    public async Task<int> ExpirePastDueAsync()
    {
        try
        {
            return await _sqlExecutor.ExecuteAsync
            (
                QueryGroupInvites.ExpirePastDueSql,
                p => p.AddWithValue(pn.Now, DateTimeOffset.UtcNow)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ExpirePastDueAsync failed");
            throw;
        }
    }
}
