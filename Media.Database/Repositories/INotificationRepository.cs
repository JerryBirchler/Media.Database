using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Notifications (WORKER-29, API-188): one per recipient of a message, never two; read by their
/// recipient; moved on to seen or dismissed, each change recorded in the audit.
/// </summary>
public interface INotificationRepository
{
    /// <summary>
    /// Notifies each of <paramref name="recipientPersonIds"/> of the message, once: a person already
    /// notified of it is skipped. Returns how many notifications were made.
    /// </summary>
    Task<int> CreateAsync(long messageId, IReadOnlyCollection<int> recipientPersonIds);

    /// <summary>
    /// A page of the person's open notifications (new or seen), newest first. <paramref name="before"/>
    /// is the last notification id of the previous page, or null for the first.
    /// </summary>
    Task<List<NotificationView>> ListOpenAsync(int recipientPersonId, long? before, int limit);

    /// <summary>
    /// Moves one of the recipient's notifications to seen (from new) or dismissed (from new or seen),
    /// and records the change in the audit in the same transaction. False when it is not theirs, does
    /// not exist, or is already past that state.
    /// </summary>
    Task<bool> SetStatusAsync(Guid notificationUuid, int recipientPersonId, NotificationStatus status);
}
