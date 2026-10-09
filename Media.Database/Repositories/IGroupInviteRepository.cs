using Media.Common.Archetypes;
using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Invitations to join a group, addressed by email address and last name (SCHEMA-35, MEDIA-8),
/// which the recipient accepts or declines from their bell before they expire.
///
/// A pending invite past its expiry is expired in every read and answer here, whether or not
/// <see cref="ExpirePastDueAsync"/> has marked it yet.
/// </summary>
public interface IGroupInviteRepository
{
    /// <summary>
    /// Invites <paramref name="emailAddress"/> (as <paramref name="lastName"/>) to
    /// <paramref name="groupId"/>, expiring <paramref name="lifetime"/> from now. When the address
    /// already has a pending invite to the group, that invite is refreshed -- new expiry, last name
    /// and inviter -- rather than a second one queued.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="lifetime"/> is not positive.</exception>
    /// <param name="audit">What queuing it is recorded as (DATABASE-66); the invite's uuid is added to its parameters.</param>
    Task<GroupInvite> CreateAsync(int groupId, int invitedByPersonId, EmailAddress emailAddress, PersonName lastName, TimeSpan lifetime, AuditEntry audit);

    /// <summary>Reads an invite by its external identifier, or null.</summary>
    Task<GroupInvite?> GetByUuidAsync(Guid groupInviteUuid);

    /// <summary>A group's pending, unexpired invites, oldest first.</summary>
    Task<List<GroupInvite>> ListPendingByGroupAsync(int groupId);

    /// <summary>
    /// The pending, unexpired invites addressed to <paramref name="emailAddress"/> -- the recipient's
    /// bell -- with each group and inviter's name, oldest first.
    /// </summary>
    Task<List<ReceivedGroupInvite>> ListPendingForEmailAsync(EmailAddress emailAddress);

    /// <summary>
    /// The recipient accepts: only a pending, unexpired invite addressed to
    /// <paramref name="recipientEmailAddress"/> changes. Returns it, or null when there is no such
    /// invite. Records the acceptance only: the membership itself is the caller's to add.
    /// </summary>
    /// <param name="audit">What accepting it is recorded as (DATABASE-66). Accepting also makes the person a member -- never demoting
    /// one already in -- and both commit with the entry, or none of them does.</param>
    Task<GroupInvite?> AcceptAsync(Guid groupInviteUuid, EmailAddress recipientEmailAddress, int acceptedByPersonId, AuditEntry audit);

    /// <summary>
    /// The recipient declines: only a pending, unexpired invite addressed to
    /// <paramref name="recipientEmailAddress"/> changes. Returns it, or null when there is no such
    /// invite.
    /// </summary>
    /// <param name="audit">What declining it is recorded as (DATABASE-66), in the same transaction.</param>
    Task<GroupInvite?> DeclineAsync(Guid groupInviteUuid, EmailAddress recipientEmailAddress, AuditEntry audit);

    /// <summary>
    /// The group's side withdraws a pending invite of <paramref name="groupId"/>. Returns it, or null
    /// when there is no such pending invite.
    /// </summary>
    /// <remarks>
    /// In one transaction (DATABASE-72): the cancel, the record as <paramref name="audit"/> naming the
    /// invite, and every open notification of it closed -- a cancelled invite leaves the invitee's
    /// bell. Nothing is recorded or closed when nothing was cancelled.
    /// </remarks>
    Task<GroupInvite?> CancelAsync(int groupId, Guid groupInviteUuid, AuditEntry audit);

    /// <summary>
    /// The Worker sweep: marks every pending invite past its expiry expired. Returns how many.
    /// </summary>
    Task<int> ExpirePastDueAsync();
}
