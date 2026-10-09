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

    /// <summary>Its recipient saw a notification (WEB-140): a change in the notification's state.</summary>
    public const string NotificationSeen = "notification.seen";

    /// <summary>Its recipient put a notification away.</summary>
    public const string NotificationDismissed = "notification.dismissed";

    /// <summary>An entry of <paramref name="kind"/>, about <paramref name="subject"/>, done by <paramref name="actor"/>.</summary>
    public static AuditEntry Entry(string kind, int subject, int actor, bool? isAdmin = null) =>
        new(kind, subject, actor, isAdmin is { } admin ? $"{{\"isAdmin\":{(admin ? "true" : "false")}}}" : null);

    /// <summary>
    /// Who is told about an entry of this kind (WORKER-29): the rule, declared beside the kind. Never
    /// the person who did it -- they know. A kind with no rule tells nobody.
    /// </summary>
    public static Told WhoIsTold(string? kind) => kind switch
    {
        MemberLeft => Told.GroupAdmins,
        MemberAdded or MemberRequestAccepted or MemberMadeAdmin or MemberMadeMember
            or MemberEnabled or MemberDisabled or MemberRemoved => Told.Subject,
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
    GroupAdmins
}
