using Media.Common.Transactions;
using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// The audit (DATABASE-63, MEDIA-53): what happened, recorded with the change itself, and read back
/// by group. Entries are never changed or removed here; archival is its own plan.
/// </summary>
public interface IAuditMessageRepository
{
    /// <summary>
    /// Records an entry and the message that says it, inside <paramref name="unitOfWork"/> -- the
    /// transaction of the change being recorded, so the change and its record commit or fail
    /// together. Returns the new entry's id.
    /// </summary>
    Task<long> RecordAsync(IUnitOfWork unitOfWork, int groupId, AuditEntry entry);

    /// <summary>
    /// A page of a group's history, newest first, each entry with its message. <paramref name="before"/>
    /// is the last entry id of the previous page, or null for the first page.
    /// </summary>
    Task<List<AuditMessage>> ListByGroupAsync(int groupId, long? before, int limit);
}
