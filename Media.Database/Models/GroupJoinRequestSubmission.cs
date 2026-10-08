namespace Media.Database.Models;

/// <summary>
/// What asking to join a group came to: the open request it landed on, and whether that request is
/// new. A repeat -- while one is pending, or after an ignore -- is absorbed into the existing request
/// (<see cref="IsNew"/> false), so a caller tells admins about a request once and never tells the
/// requester that they were ignored.
/// </summary>
public record GroupJoinRequestSubmission(GroupJoinRequest Request, bool IsNew);
