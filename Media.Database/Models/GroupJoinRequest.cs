namespace Media.Database.Models;

/// <summary>
/// A person asking to join a group they named exactly (SCHEMA-35, MEDIA-8). Groups cannot be
/// browsed or discovered, so knowing the exact name is the only way to ask.
/// </summary>
public record GroupJoinRequest
{
    public int GroupJoinRequestId { get; init; }

    /// <summary>The request's external identifier -- what an admin answers it by.</summary>
    public Guid GroupJoinRequestUuid { get; init; }

    public int GroupId { get; init; }

    /// <summary>The requester.</summary>
    public int PersonId { get; init; }

    public GroupJoinRequestStatus Status { get; init; }

    /// <summary>The admin who answered; null while pending.</summary>
    public int? AnsweredByPersonId { get; init; }

    /// <summary>When it was answered; null while pending.</summary>
    public DateTimeOffset? AnsweredOn { get; init; }

    /// <summary>
    /// When a pending request stops waiting (SCHEMA-39): fourteen days after it was asked, by
    /// Media.Api's setting. Past it, it is as good as gone -- out of the admins' lists, unanswerable
    /// -- and retired as <see cref="GroupJoinRequestStatus.Expired"/> when the person asks again.
    /// An answer, a ban included, never expires.
    /// </summary>
    public DateTimeOffset ExpiresOn { get; init; }

    public DateTimeOffset InsertedOn { get; init; }

    public DateTimeOffset? UpdatedOn { get; init; }
}

/// <summary>
/// Where a join request stands. Stored as its integer value; the values are the schema's
/// (Media.Schema Create_Table_GroupJoinRequests.sql), so never renumber them.
/// </summary>
public enum GroupJoinRequestStatus
{
    /// <summary>Asked, not answered yet.</summary>
    Pending = 0,

    /// <summary>An admin let them in; the membership itself is a GroupsPersons row.</summary>
    Accepted = 1,

    /// <summary>An admin said no. The requester is told, and may ask again (a new request).</summary>
    Rejected = 2,

    /// <summary>
    /// An admin chose not to answer, for good. The requester is never told, and every further
    /// request from them to this group is absorbed into this one in silence.
    /// </summary>
    Ignored = 3,

    /// <summary>Nobody answered before it expired (SCHEMA-39); the person may ask afresh.</summary>
    Expired = 4
}
