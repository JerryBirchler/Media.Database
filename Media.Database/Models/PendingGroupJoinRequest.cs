namespace Media.Database.Models;

/// <summary>
/// A pending join request as a group's admins see it: who is asking, by name and email, and since
/// when. Only external identifiers -- no internal ids leave through this shape.
/// </summary>
public record PendingGroupJoinRequest
{
    public Guid GroupJoinRequestUuid { get; init; }

    /// <summary>The requester's external identifier.</summary>
    public Guid PersonUuid { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string EmailAddress { get; init; } = string.Empty;

    /// <summary>When they asked.</summary>
    public DateTimeOffset InsertedOn { get; init; }
}
