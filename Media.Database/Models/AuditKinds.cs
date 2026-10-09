namespace Media.Database.Models;

/// <summary>
/// The catalogue of what is recorded in the audit (MEDIA-53): each kind declared once, here, as the
/// voice vocabulary declares its words once -- shared by Media.Api, which records them, and
/// Media.Worker, which tells people about them. A kind names what happened, not how it was asked
/// for -- an admin removing someone and someone leaving end the membership the same way, and are
/// different entries. Its words are the reader's to render, in their language; the people and group
/// it is about are the entry's own columns, never copied into it.
/// </summary>
public static class AuditKinds
{
    /// <summary>A group was made, its founder its first admin.</summary>
    public const string GroupCreated = "group.created";

    /// <summary>An admin added someone (by details, or with a new person) -- as an admin when <c>isAdmin</c> says so.</summary>
    public const string MemberAdded = "member.added";

    /// <summary>An admin accepted someone's request to join.</summary>
    public const string MemberRequestAccepted = "member.request-accepted";

    /// <summary>A member was made an admin.</summary>
    public const string MemberMadeAdmin = "member.made-admin";

    /// <summary>An admin was made a member only.</summary>
    public const string MemberMadeMember = "member.made-member";

    /// <summary>An admin turned a disabled member back on.</summary>
    public const string MemberEnabled = "member.enabled";

    /// <summary>An admin disabled a member.</summary>
    public const string MemberDisabled = "member.disabled";

    /// <summary>An admin removed a member.</summary>
    public const string MemberRemoved = "member.removed";

    /// <summary>A member left the group on their own.</summary>
    public const string MemberLeft = "member.left";

    /// <summary>
    /// An admin invited someone by last name and email (DATABASE-66). Its parameters carry the
    /// invite's uuid -- for the Worker and the Api only, never shown to a client.
    /// </summary>
    public const string InviteQueued = "invite.queued";

    /// <summary>The person invited accepted, and is a member.</summary>
    public const string InviteAccepted = "invite.accepted";

    /// <summary>The person invited declined.</summary>
    public const string InviteDeclined = "invite.declined";

    /// <summary>
    /// A member sent a note to others they chose (DATABASE-73): told to them, never the sender. Its
    /// parameters carry the note, shown as written, and the recipients -- for the Worker only,
    /// never shown to a client.
    /// </summary>
    public const string NoteSent = "note.sent";

    /// <summary>
    /// An admin cancelled a pending invite (DATABASE-72): history only -- the invite simply leaves
    /// the invitee's bell, and nobody is told.
    /// </summary>
    public const string InviteCancelled = "invite.cancelled";

    /// <summary>
    /// Someone asked to join a group by its name (DATABASE-67). Its parameters carry the request's
    /// uuid -- for the Worker and the Api only, never shown to a client.
    /// </summary>
    public const string RequestQueued = "request.queued";

    /// <summary>
    /// An admin let in someone who asked (DATABASE-68): history only -- the person is told by
    /// <see cref="MemberRequestAccepted"/>, recorded with their membership.
    /// </summary>
    public const string RequestAccepted = "request.accepted";

    /// <summary>An admin turned down someone who asked; they are told, and may ask again.</summary>
    public const string RequestDeclined = "request.declined";

    /// <summary>
    /// An admin ignored someone's requests for good, for the whole group: the person is never told,
    /// and later requests are absorbed. Every other admin is told -- one admin decided for all.
    /// </summary>
    public const string RequestIgnored = "request.ignored";

    /// <summary>
    /// An admin muted someone's requests to a group, for themselves alone (DATABASE-69): history
    /// only -- nobody is told, and the request stays pending for the other admins.
    /// </summary>
    public const string RequestMuted = "request.muted";

    /// <summary>Its recipient saw a notification (WEB-140): a change in the notification's state.</summary>
    public const string NotificationSeen = "notification.seen";

    /// <summary>Its recipient put a notification away.</summary>
    public const string NotificationDismissed = "notification.dismissed";

    /// <summary>Its recipient answered a notification's decision (accepted, declined): it is done.</summary>
    public const string NotificationActed = "notification.acted";

    /// <summary>The parameter naming the invite an invite.* entry is about.</summary>
    public const string InviteUuidParameter = "inviteUuid";

    /// <summary>The parameter naming the request to join a request.* entry is about.</summary>
    public const string RequestUuidParameter = "requestUuid";

    /// <summary>
    /// The parameter carrying an admin's own words with an answer (a decline's note): shown as
    /// written, never translated -- a novel message, not a catalogued one.
    /// </summary>
    public const string NoteParameter = "note";

    /// <summary>The parameter naming a note's recipients, by person id (DATABASE-73): never shown to a client.</summary>
    public const string RecipientsParameter = "recipients";

    /// <summary>The longest a note may be.</summary>
    public const int NoteMaxLength = 280;

    /// <summary>An entry of <paramref name="kind"/>, about <paramref name="subject"/>, done by <paramref name="actor"/>.</summary>
    public static AuditEntry Entry(string kind, int subject, int actor, bool? isAdmin = null) =>
        new(kind, subject, actor, isAdmin is { } admin ? $"{{\"isAdmin\":{(admin ? "true" : "false")}}}" : null);

    /// <summary>
    /// Who is told about an entry of this kind (WORKER-29): the rule, declared beside the kind. Never
    /// the person who did it -- they know. A kind with no rule tells nobody.
    /// </summary>
    public static Told WhoIsTold(string? kind) => kind switch
    {
        MemberLeft or InviteAccepted or InviteDeclined or RequestQueued or RequestIgnored => Told.GroupAdmins,
        InviteQueued => Told.Invitee,
        NoteSent => Told.Named,
        MemberAdded or MemberRequestAccepted or MemberMadeAdmin or MemberMadeMember
            or MemberEnabled or MemberDisabled or MemberRemoved or RequestDeclined => Told.Subject,
        _ => Told.Nobody
    };
}

/// <summary>Who an audit entry's notifications go to (WORKER-29).</summary>
public enum Told
{
    /// <summary>Nobody: the entry is history only.</summary>
    Nobody,

    /// <summary>The person it happened to.</summary>
    Subject,

    /// <summary>The group's active admins.</summary>
    GroupAdmins,

    /// <summary>
    /// Whoever an invite names: the active persons with its last name and email -- found after the
    /// fact, so queuing an invite answers the same whether or not anyone has those details.
    /// </summary>
    Invitee,

    /// <summary>
    /// The people the entry names itself (DATABASE-73): a note's recipients, those still active
    /// members of the group.
    /// </summary>
    Named
}
