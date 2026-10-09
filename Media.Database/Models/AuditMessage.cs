namespace Media.Database.Models;

/// <summary>
/// One entry of the audit (DATABASE-63, MEDIA-53): what happened, to whom, by whom, in which group,
/// when -- with the message that says it. Never changed once written.
/// </summary>
public record AuditMessage
{
    public long AuditMessageId { get; init; }

    public required Guid AuditMessageUuid { get; init; }

    /// <summary>The catalogued kind of the message, or null for a literal one.</summary>
    public string? Kind { get; init; }

    /// <summary>The values a catalogued message is said with, as a JSON object, or null.</summary>
    public string? Parameters { get; init; }

    /// <summary>The words of a message nothing catalogues, or null.</summary>
    public string? Text { get; init; }

    public required string Language { get; init; }

    public int? GroupId { get; init; }

    public int? SubjectPersonId { get; init; }

    /// <summary>Who did it; null when the system did.</summary>
    public int? ActorPersonId { get; init; }

    /// <summary>The notification this entry is about, for a change in a notification's state.</summary>
    public long? NotificationId { get; init; }

    public required DateTimeOffset InsertedOn { get; init; }
}
