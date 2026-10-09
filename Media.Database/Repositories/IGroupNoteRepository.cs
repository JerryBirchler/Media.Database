using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// A note from one member of a group to others they chose (DATABASE-73): recorded in the audit,
/// like everything that happens in a group, and told to the people it names by Media.Worker.
/// </summary>
public interface IGroupNoteRepository
{
    /// <summary>
    /// Records <paramref name="note"/> from <paramref name="senderPersonId"/> to
    /// <paramref name="recipientPersonIds"/> in <paramref name="groupId"/> as one note.sent entry, in
    /// its own transaction: the words shown as written, the recipients kept in the entry's private
    /// parameters, never shown to a client. Returns the entry's id.
    /// </summary>
    Task<long> SendAsync(int groupId, int senderPersonId, IReadOnlyCollection<int> recipientPersonIds, string note);
}
