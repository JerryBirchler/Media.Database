namespace Media.Database.Models;

/// <summary>Where a notification stands (SCHEMA-36): its whole lifecycle, each change itself audited.</summary>
public enum NotificationStatus
{
    New = 0,
    Seen = 1,
    Dismissed = 2,
    Acted = 3,
    Expired = 4,
    Answered = 5
}

/// <summary>
/// A notification as its recipient reads it (API-188): its state, and the message it refers to with
/// what that message is about -- the group and the people, by the names they go by now. Nothing here
/// is copied from the message; the read joins it.
/// </summary>
public record NotificationView
{
    public long NotificationId { get; init; }

    public required Guid NotificationUuid { get; init; }

    public required NotificationStatus Status { get; init; }

    public bool IsPinned { get; init; }

    public DateTimeOffset? ExpiresOn { get; init; }

    public required DateTimeOffset InsertedOn { get; init; }

    /// <summary>The message's catalogued kind, or null for a literal one.</summary>
    public string? Kind { get; init; }

    /// <summary>The values the message is said with, as JSON text, or null.</summary>
    public string? Parameters { get; init; }

    /// <summary>The literal words of a message nothing catalogues, or null.</summary>
    public string? Text { get; init; }

    public required string Language { get; init; }

    /// <summary>The group it is about, by title, or null.</summary>
    public string? GroupTitle { get; init; }

    /// <summary>The person it happened to, by the name they go by, or null.</summary>
    public string? SubjectName { get; init; }

    /// <summary>The person who did it, by the name they go by, or null for the system.</summary>
    public string? ActorName { get; init; }
}
