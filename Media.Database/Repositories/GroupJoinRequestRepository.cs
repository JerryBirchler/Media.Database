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
    private static string RequestParameters(GroupJoinRequest request) =>
        JsonSerializer.Serialize(new Dictionary<string, Guid> { [AuditKinds.RequestUuidParameter] = request.GroupJoinRequestUuid });

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

    public async Task<List<PendingGroupJoinRequest>> ListPendingByGroupAsync(int groupId)
    {
        try
        {
            return await _sqlExecutor.QueryManyAsync
            (
                QueryGroupJoinRequests.ListPendingByGroupSql,
                p => p.AddWithValue(pn.GroupId, groupId),
                reader => reader.ToPendingGroupJoinRequest()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ListPendingByGroupAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<GroupJoinRequest?> AnswerAsync(int groupId, Guid groupJoinRequestUuid, GroupJoinRequestStatus answer, int answeredByPersonId)
    {
        if (answer is GroupJoinRequestStatus.Pending || !Enum.IsDefined(answer))
            throw new ArgumentOutOfRangeException(nameof(answer), answer, "An answer is Accepted, Rejected or Ignored.");

        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupJoinRequests.AnswerSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.GroupJoinRequestUuid, groupJoinRequestUuid);
                    p.AddWithValue(pn.Status, (int)answer);
                    p.AddWithValue(pn.AnsweredByPersonId, answeredByPersonId);
                    p.AddWithValue(pn.Now, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupJoinRequest()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AnswerAsync failed for GroupId: [{GroupId}], GroupJoinRequestUuid: [{GroupJoinRequestUuid}], Answer: [{Answer}]",
                groupId, groupJoinRequestUuid, answer);
            throw;
        }
    }
}
