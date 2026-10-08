namespace Media.Database.Models;

/// <summary>
/// An admin's invitation for someone to join a group, addressed by email address and last name --
/// never a cellphone number (SCHEMA-35, MEDIA-8). The recipient accepts or declines it from their
/// bell before <see cref="ExpiresOn"/>.
/// </summary>
public record GroupInvite
{
    public int GroupInviteId { get; init; }

    /// <summary>The invite's external identifier -- what the recipient or an admin acts on it by.</summary>
    public Guid GroupInviteUuid { get; init; }

    public int GroupId { get; init; }

    public int InvitedByPersonId { get; init; }

    /// <summary>The address invited, compared case-insensitively as Persons compares it.</summary>
    public string EmailAddress { get; init; } = string.Empty;

    /// <summary>The last name invited, compared case-insensitively as Persons compares it.</summary>
    public string LastName { get; init; } = string.Empty;

    public GroupInviteStatus Status { get; init; }

    /// <summary>
    /// When the invite runs out, fixed from the configured lifetime when it was made. A pending
    /// invite past this is treated as expired before the sweep marks it so.
    /// </summary>
    public DateTimeOffset ExpiresOn { get; init; }

    /// <summary>The person who accepted it; set only when <see cref="Status"/> is Accepted.</summary>
    public int? AcceptedByPersonId { get; init; }

    /// <summary>When it was accepted, declined or cancelled; null while pending, and when expired.</summary>
    public DateTimeOffset? AnsweredOn { get; init; }

    public DateTimeOffset InsertedOn { get; init; }

    public DateTimeOffset? UpdatedOn { get; init; }
}

/// <summary>
/// Where an invite stands. Stored as its integer value; the values are the schema's (Media.Schema
/// Create_Table_GroupInvites.sql), so never renumber them.
/// </summary>
public enum GroupInviteStatus
{
    /// <summary>Waiting for the recipient.</summary>
    Pending = 0,

    /// <summary>The recipient joined.</summary>
    Accepted = 1,

    /// <summary>The recipient said no.</summary>
    Declined = 2,

    /// <summary>It ran out before an answer.</summary>
    Expired = 3,

    /// <summary>The inviting side withdrew it.</summary>
    Cancelled = 4
}
