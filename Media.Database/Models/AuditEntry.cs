namespace Media.Database.Models;

/// <summary>
/// What a change is recorded as (DATABASE-63, MEDIA-53): the catalogued kind of message that says
/// it, who it happened to, who did it (null when the system did), and any values the message is
/// said with beyond those. The group is the one the change is made in, so the repository making the
/// change supplies it.
/// </summary>
/// <param name="Kind">The catalogued kind, e.g. "member.left".</param>
/// <param name="SubjectPersonId">The person it happened to, if a person.</param>
/// <param name="ActorPersonId">The person who did it, or null for the system.</param>
/// <param name="Parameters">Further values the message is said with, as a JSON object; null for none.</param>
public record AuditEntry(string Kind, int? SubjectPersonId, int? ActorPersonId, string? Parameters = null);
