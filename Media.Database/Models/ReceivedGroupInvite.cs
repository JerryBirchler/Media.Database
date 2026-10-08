namespace Media.Database.Models;

/// <summary>
/// A pending invite as its recipient sees it in their bell: which group, who invited them, and until
/// when. Only external identifiers -- no internal ids leave through this shape.
/// </summary>
public record ReceivedGroupInvite
{
    public Guid GroupInviteUuid { get; init; }

    public Guid GroupUuid { get; init; }

    public string GroupName { get; init; } = string.Empty;

    public string GroupTitle { get; init; } = string.Empty;

    public string InvitedByFirstName { get; init; } = string.Empty;

    public string InvitedByLastName { get; init; } = string.Empty;

    public DateTimeOffset ExpiresOn { get; init; }

    public DateTimeOffset InsertedOn { get; init; }
}
