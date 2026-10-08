namespace Media.Database.Models;

/// <summary>
/// The misses an admin has made adding members by last name and email (SCHEMA-35, MEDIA-8). See
/// <see cref="MemberAddStrikePolicy"/> for how a miss changes it.
/// </summary>
public record PersonMemberAddStrikes
{
    /// <summary>The admin; one row per person.</summary>
    public int PersonId { get; init; }

    /// <summary>Misses inside the current window; zero once the window has ended or was cleared.</summary>
    public int StrikeCount { get; init; }

    /// <summary>When the count goes away by itself; null exactly when the count is zero.</summary>
    public DateTimeOffset? WindowEndsOn { get; init; }

    /// <summary>
    /// When the third miss locked the account. Only a full re-registration clears it -- never the
    /// window ending.
    /// </summary>
    public DateTimeOffset? LockedOn { get; init; }

    public DateTimeOffset InsertedOn { get; init; }

    public DateTimeOffset? UpdatedOn { get; init; }

    /// <summary>Whether the account is locked by misses.</summary>
    public bool IsLocked => LockedOn is not null;
}
