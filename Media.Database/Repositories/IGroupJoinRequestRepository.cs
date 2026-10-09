using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Requests to join a group by its exact name (SCHEMA-35, MEDIA-8), and the admins' answers.
/// </summary>
public interface IGroupJoinRequestRepository
{
    /// <summary>
    /// Asks for <paramref name="personId"/> to join <paramref name="groupId"/>. When the person
    /// already has an open request to the group -- pending, or ignored for good -- the repeat is
    /// absorbed into it: nothing is written and that request is returned with
    /// <see cref="GroupJoinRequestSubmission.IsNew"/> false. A new request is recorded as
    /// <paramref name="audit"/> in the same transaction, naming the request in its parameters; a
    /// repeat records nothing.
    /// </summary>
    Task<GroupJoinRequestSubmission> SubmitAsync(int groupId, int personId, AuditEntry audit);

    /// <summary>Reads a request by its external identifier, or null.</summary>
    Task<GroupJoinRequest?> GetByUuidAsync(Guid groupJoinRequestUuid);

    /// <summary>
    /// A group's pending requests, oldest first, with each active requester's name and email.
    /// </summary>
    Task<List<PendingGroupJoinRequest>> ListPendingByGroupAsync(int groupId);

    /// <summary>
    /// Answers a pending request of <paramref name="groupId"/> -- accepted, rejected or ignored --
    /// recording <paramref name="answeredByPersonId"/> and the moment. Returns the answered request,
    /// or null when there is no pending request of that group by that identifier (unknown, another
    /// group's, or answered already). Accepting records the answer only: the membership itself is
    /// the caller's to add.
    /// <para>
    /// In the same transaction (DATABASE-68): the answer is recorded as <paramref name="audit"/>,
    /// about the person who asked and naming the request; and every admin's open notification of
    /// the request is closed as acted, so nobody is left asked about a request already answered.
    /// Nothing is recorded or closed when nothing was answered.
    /// </para>
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="answer"/> is Pending, which is not an answer.</exception>
    Task<GroupJoinRequest?> AnswerAsync(int groupId, Guid groupJoinRequestUuid, GroupJoinRequestStatus answer, int answeredByPersonId, AuditEntry audit);
}
