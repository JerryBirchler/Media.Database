using Media.Common.Helpers.Fluent;
using Media.Common.Transactions;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.Json.Nodes;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupJoinRequestRepository"/>
/// <remarks>
/// Writes go to PostgreSQL only; Scylla's copy follows through CDC
/// (<see cref="Cdc.GroupJoinRequestsCdcSyncHandler"/>).
/// </remarks>
public class GroupJoinRequestRepository(
    ISqlQueryExecutor sqlExecutor,
    IAuditMessageRepository auditMessageRepository,
    Func<IUnitOfWork> unitOfWorkFactory,
    ILogger<GroupJoinRequestRepository> logger)
    : IGroupJoinRequestRepository
{
    /// <summary>
    /// How many times a submission tries before giving up. A second try only happens when the open
    /// request it collided with was answered in between, so two is already generous.
    /// </summary>
    private const int SubmitAttempts = 3;

    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IAuditMessageRepository _auditMessageRepository = auditMessageRepository;
    private readonly Func<IUnitOfWork> _unitOfWorkFactory = unitOfWorkFactory;
    private readonly FluentLogger<GroupJoinRequestRepository> _logger = logger.Initializer();

    public async Task<GroupJoinRequestSubmission> SubmitAsync(int groupId, int personId, AuditEntry audit)
    {
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            for (var attempt = 0; attempt < SubmitAttempts; attempt++)
            {
                var added = await _sqlExecutor.QuerySingleAsync
                (
                    uow,
                    QueryGroupJoinRequests.AddSql,
                    p =>
                    {
                        p.AddWithValue(pn.GroupId, groupId);
                        p.AddWithValue(pn.PersonId, personId);
                    },
                    reader => reader.ToGroupJoinRequest()
                );

                if (added is not null)
                {
                    // The request and its record together (DATABASE-67): the admins are told from the record.
                    await _auditMessageRepository.RecordAsync(uow, groupId, audit with { Parameters = RequestParameters(added) });
                    await uow.CommitAsync();
                    return new GroupJoinRequestSubmission(added, IsNew: true);
                }

                // Absorbed: an open request is already there. Read it -- unless it was answered in
                // the instant between, in which case there is room for a new one, so go round again.
                var open = await _sqlExecutor.QuerySingleAsync
                (
                    uow,
                    QueryGroupJoinRequests.GetOpenSql,
                    p =>
                    {
                        p.AddWithValue(pn.GroupId, groupId);
                        p.AddWithValue(pn.PersonId, personId);
                    },
                    reader => reader.ToGroupJoinRequest()
                );

                if (open is not null)
                {
                    await uow.CommitAsync();
                    return new GroupJoinRequestSubmission(open, IsNew: false);
                }
            }

            throw new InvalidOperationException(
                $"A join request for GroupId {groupId} and PersonId {personId} neither inserted nor was found open after {SubmitAttempts} attempts.");
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "SubmitAsync failed for GroupId: [{GroupId}], PersonId: [{PersonId}]", groupId, personId);
            throw;
        }
    }

    /// <summary>The request an entry is about, as its parameters: for the Worker and the Api, never shown to a client.</summary>
    /// <remarks>Whatever the entry already carries -- a decline's note -- is kept beside it.</remarks>
    private static string RequestParameters(GroupJoinRequest request, string? carried = null)
    {
        var parameters = (carried is null ? null : JsonNode.Parse(carried) as JsonObject) ?? [];
        parameters[AuditKinds.RequestUuidParameter] = request.GroupJoinRequestUuid;
        return parameters.ToJsonString();
    }

    public async Task<GroupJoinRequest?> GetByUuidAsync(Guid groupJoinRequestUuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupJoinRequests.GetByUuidSql,
                p => p.AddWithValue(pn.GroupJoinRequestUuid, groupJoinRequestUuid),
                reader => reader.ToGroupJoinRequest()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByUuidAsync failed for GroupJoinRequestUuid: [{GroupJoinRequestUuid}]", groupJoinRequestUuid);
            throw;
        }
    }

    public async Task<List<PendingGroupJoinRequest>> ListPendingByGroupAsync(int groupId, int viewerPersonId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryGroupJoinRequests.ListPendingByGroupSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.MutedByPersonId, viewerPersonId);
                },
                reader => reader.ToPendingGroupJoinRequest()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListPendingByGroupAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<GroupJoinRequest?> MuteAsync(int groupId, Guid groupJoinRequestUuid, int mutedByPersonId, AuditEntry audit)
    {
        var now = DateTimeOffset.UtcNow;
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            // Only a request still waiting, and only of this group.
            var request = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryGroupJoinRequests.GetByUuidSql,
                p => p.AddWithValue(pn.GroupJoinRequestUuid, groupJoinRequestUuid),
                reader => reader.ToGroupJoinRequest()
            );

            if (request is not { Status: GroupJoinRequestStatus.Pending } || request.GroupId != groupId)
            {
                await uow.RollbackAsync();
                return null;
            }

            await _sqlExecutor.ExecuteAsync
            (
                uow,
                QueryGroupJoinRequests.MuteSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.PersonId, request.PersonId);
                    p.AddWithValue(pn.MutedByPersonId, mutedByPersonId);
                }
            );

            await _auditMessageRepository.RecordAsync(uow, groupId,
                audit with { SubjectPersonId = request.PersonId, Parameters = RequestParameters(request, audit.Parameters) });

            // That admin is asked no more; the other admins still are.
            await _sqlExecutor.QuerySingleValueAsync
            (
                uow,
                QueryGroupJoinRequests.CloseNotificationsOfSql,
                p =>
                {
                    p.AddWithValue(pn.GroupJoinRequestUuid, request.GroupJoinRequestUuid);
                    p.AddWithValue(pn.MutedByPersonId, mutedByPersonId);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.GetFieldValue<long>(0)
            );

            await uow.CommitAsync();
            return request;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "MuteAsync failed for GroupId: [{GroupId}], GroupJoinRequestUuid: [{GroupJoinRequestUuid}]", groupId, groupJoinRequestUuid);
            throw;
        }
    }

    public async Task<List<int>> ListMutedByAsync(int groupId, int personId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryGroupJoinRequests.ListMutedBySql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.PersonId, personId);
                },
                reader => reader.GetFieldValue<int>(0)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListMutedByAsync failed for GroupId: [{GroupId}], PersonId: [{PersonId}]", groupId, personId);
            throw;
        }
    }

    public async Task<GroupJoinRequest?> AnswerAsync(int groupId, Guid groupJoinRequestUuid, GroupJoinRequestStatus answer, int answeredByPersonId, AuditEntry audit)
    {
        if (answer is GroupJoinRequestStatus.Pending || !Enum.IsDefined(answer))
            throw new ArgumentOutOfRangeException(nameof(answer), answer, "An answer is Accepted, Rejected or Ignored.");

        var now = DateTimeOffset.UtcNow;
        await using var uow = _unitOfWorkFactory();

        try
        {
            await uow.BeginTransactionAsync();

            var answered = await _sqlExecutor.QuerySingleAsync
            (
                uow,
                QueryGroupJoinRequests.AnswerSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.GroupJoinRequestUuid, groupJoinRequestUuid);
                    p.AddWithValue(pn.Status, (int)answer);
                    p.AddWithValue(pn.AnsweredByPersonId, answeredByPersonId);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.ToGroupJoinRequest()
            );

            if (answered is null)
            {
                await uow.RollbackAsync();
                return null;
            }

            // The answer, about the person who asked (DATABASE-68) -- known only now, from the request.
            await _auditMessageRepository.RecordAsync(uow, groupId,
                audit with { SubjectPersonId = answered.PersonId, Parameters = RequestParameters(answered, audit.Parameters) });

            // Nobody is left asked about it: every admin's notification of the request is done.
            await _sqlExecutor.QuerySingleValueAsync
            (
                uow,
                QueryGroupJoinRequests.CloseNotificationsSql,
                p =>
                {
                    p.AddWithValue(pn.GroupJoinRequestUuid, answered.GroupJoinRequestUuid);
                    p.AddWithValue(pn.Now, now);
                },
                reader => reader.GetFieldValue<long>(0)
            );

            await uow.CommitAsync();
            return answered;
        }
        catch (Exception ex)
        {
            await uow.RollbackAsync();
            _logger.LogError(ex, "AnswerAsync failed for GroupId: [{GroupId}], GroupJoinRequestUuid: [{GroupJoinRequestUuid}], Answer: [{Answer}]",
                groupId, groupJoinRequestUuid, answer);
            throw;
        }
    }
}
