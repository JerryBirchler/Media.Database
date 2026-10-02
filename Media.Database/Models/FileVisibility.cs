namespace Media.Database.Models;

/// <summary>
/// Whose files a caller may see. Built only through <see cref="ForDevice"/> or
/// <see cref="ForGroup"/>, so "any file" cannot be expressed (API-148): search once returned every
/// tenant's files, because an empty device filter meant "any" and nothing else restricted it.
///
/// The same rule as the word queries' scope (MEDIA-34, QueryWords.AndScope): a device credential
/// sees its own device's files; a group credential sees the files of every device active in its
/// group. A class rather than a struct, so there is no <c>default</c> that is neither.
/// </summary>
public sealed record FileVisibility
{
    private FileVisibility(int? sourceMachineId, int? groupId)
    {
        SourceMachineId = sourceMachineId;
        GroupId = groupId;
    }

    /// <summary>The one device whose files are visible, when the caller is a device.</summary>
    public int? SourceMachineId { get; }

    /// <summary>The group whose active devices' files are visible, when the caller is a group member.</summary>
    public int? GroupId { get; }

    public static FileVisibility ForDevice(int sourceMachineId) => new(sourceMachineId, null);

    public static FileVisibility ForGroup(int groupId) => new(null, groupId);
}
