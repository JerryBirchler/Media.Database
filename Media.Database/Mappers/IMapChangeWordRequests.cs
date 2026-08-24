using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Interface for mapping word changes to change requests.
/// </summary>
public interface IMapChangeWordRequests
{
    /// <summary>
    /// Processes a list of words by comparing current and new lists and generating change requests.
    /// </summary>
    /// <param name="updates">The list to populate with change requests.</param>
    /// <param name="curList">The current list of words.</param>
    /// <param name="newList">The new list of words.</param>
    /// <param name="current">The current file context.</param>
    /// <param name="origin">The origin of the words.</param>
    void ProcessList(
        List<ChangeWordRequest> updates,
        IEnumerable<string>? curList,
        IEnumerable<string>? newList,
        Files current,
        WordOrigin origin);

    /// <summary>
    /// Processes a single word by comparing current and new values and generating change requests.
    /// </summary>
    /// <param name="updates">The list to populate with change requests.</param>
    /// <param name="curValue">The current word value.</param>
    /// <param name="newValue">The new word value.</param>
    /// <param name="current">The current file context.</param>
    /// <param name="origin">The origin of the word.</param>
    void ProcessScalar(
        List<ChangeWordRequest> updates,
        string? curValue,
        string? newValue,
        Files current,
        WordOrigin origin);
}