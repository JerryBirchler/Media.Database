using Media.Common.Archetypes;
using Media.Common.Helpers.Fluent;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
    IAuditMessageRepository auditMessageRepository,
    Func<IUnitOfWork> unitOfWorkFactory,
    ILogger<GroupInviteRepository> logger)
    : IGroupInviteRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IAuditMessageRepository _auditMessageRepository = auditMessageRepository;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<GroupInviteRepository> _logger = logger.Initializer();

    public async Task<GroupInvite> CreateAsync(int groupId, int invitedByPersonId, EmailAddress emailAddress, PersonName lastName, TimeSpan lifetime, AuditEntry audit)
    {
        if (lifetime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(lifetime), lifetime, "An invite's lifetime must be positive.");

        var now = DateTimeOffset.UtcNow;
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var invite = await _sqlExecutor.QuerySingleAsync
            (
                uow,
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
            )
            // An insert or an upsert always returns its row; nothing back means the statement did not run.
            ?? throw new InvalidOperationException($"Creating an invite to GroupId {groupId} returned no row.");

            // Who it names is found later, by the Worker, from the invite this points at (DATABASE-66).
            await _auditMessageRepository.RecordAsync(uow, groupId, audit with { Parameters = InviteParameters(invite) });

            await uow.CommitAsync();
            return invite;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "CreateAsync failed for GroupId: [{GroupId}], InvitedByPersonId: [{InvitedByPersonId}]", groupId, invitedByPersonId);
            throw;
        }
    }

    /// <summary>The invite an entry is about, as its parameters: for the Worker and the Api, never shown to a client.</summary>
    private static string InviteParameters(GroupInvite invite) =>
        JsonSerializer.Serialize(new Dictionary<string, Guid> { [AuditKinds.InviteUuidParameter] = invite.GroupInviteUuid });

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

    public async Task<GroupInvite?> AcceptAsync(Guid groupInviteUuid, EmailAddress recipientEmailAddress, int acceptedByPersonId, AuditEntry audit)
    {
        var now = DateTimeOffset.UtcNow;
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var accepted = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryGroupInvites.AcceptSql,
                p =>
                {
                    p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid);
                    p.AddWithValue(pn.EmailAddress, recipientEmailAddress.ToString());
                    p.AddWithValue(pn.AcceptedByPersonId, acceptedByPersonId);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.ToGroupInvite()
            );

            if (accepted is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            // A member already stays exactly as they are -- above all, an admin is never demoted.
            var current = await _sqlExecutor.QuerySingleValueAsync
            (
                uow,
                QueryGroupsPersons.CountActiveMembershipSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, accepted.GroupId);
                    p.AddWithValue(pn.PersonId, acceptedByPersonId);
                },
                reader => reader.GetInt64(0)
            );

            if (current is not > 0)
            {
                await _sqlExecutor.ExecuteAsync
                (
                    uow,
                    QueryGroupsPersons.UpsertSql,
                    p =>
                    {
                        p.AddWithValue(pn.GroupId, accepted.GroupId);
                        p.AddWithValue(pn.PersonId, acceptedByPersonId);
                        p.AddWithValue(pn.IsAdmin, false);
                        p.AddWithValue(pn.UpdatedOn, now);
                    }
                );
            }

            await _auditMessageRepository.RecordAsync(uow, accepted.GroupId, audit with { Parameters = InviteParameters(accepted) });

            await uow.CommitAsync();
            return accepted;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "AcceptAsync failed for GroupInviteUuid: [{GroupInviteUuid}], AcceptedByPersonId: [{AcceptedByPersonId}]", groupInviteUuid, acceptedByPersonId);
            throw;
        }
    }

    public async Task<GroupInvite?> DeclineAsync(Guid groupInviteUuid, EmailAddress recipientEmailAddress, AuditEntry audit)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var declined = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryGroupInvites.DeclineSql,
                p =>
                {
                    p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid);
                    p.AddWithValue(pn.EmailAddress, recipientEmailAddress.ToString());
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupInvite()
            );

            if (declined is not null)
                await _auditMessageRepository.RecordAsync(uow, declined.GroupId, audit with { Parameters = InviteParameters(declined) });

            await uow.CommitAsync();
            return declined;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "DeclineAsync failed for GroupInviteUuid: [{GroupInviteUuid}]", groupInviteUuid);
            throw;
        }
    }

    public async Task<GroupInvite?> CancelAsync(int groupId, Guid groupInviteUuid, AuditEntry audit)
    {
        var now = DateTimeOffset.UtcNow;
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var cancelled = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryGroupInvites.CancelSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.GroupInviteUuid, groupInviteUuid);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.ToGroupInvite()
            );

            if (cancelled is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            await _auditMessageRepository.RecordAsync(uow, groupId, audit with { Parameters = InviteParameters(cancelled) });

            // Out of the invitee's bell: a cancelled invite cannot be accepted from there.
            await _sqlExecutor.QuerySingleValueAsync
            (
                uow,
                QueryGroupInvites.CloseNotificationsSql,
                p =>
                {
                    p.AddWithValue(pn.GroupInviteUuid, cancelled.GroupInviteUuid);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.GetFieldValue<long>(0)
            );

            await uow.CommitAsync();
            return cancelled;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
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
