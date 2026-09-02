using Media.Database.Models;

namespace Media.Database.Mappers;

/// <summary>
/// Implementation of IMapChangeWordRequests that generates change requests by comparing word lists and values.
/// </summary>
public class MapChangeWordRequests : IMapChangeWordRequests
{
    /// <summary>
    /// Processes a list of words by comparing current and new lists and generating appropriate change requests (delete, upsert).
    /// </summary>
    /// <param name="updates">The list to populate with change requests.</param>
    /// <param name="curList">The current list of words.</param>
    /// <param name="newList">The new list of words.</param>
    /// <param name="current">The current file context.</param>
    /// <param name="origin">The origin of the words.</param>
    public void ProcessList(
        List<ChangeWordRequest> updates,
        IEnumerable<string>? curList,
        IEnumerable<string>? newList,
        Files current,
        WordOrigin origin)
    {
        var curSet = new HashSet<string>(curList ?? []);
        var newSet = new HashSet<string>(newList ?? []);

        foreach (var item in curSet.Except(newSet))
        {
            updates.Add(new ChangeWordRequest
            {
                Action = WordProducerActions.Delete,
                Origin = origin,
                NewSpan = item,
                CameFromFileId = current.Id
            });
        }

        foreach (var item in newSet.Except(curSet))
        {
            updates.Add(new ChangeWordRequest
            {
                Action = WordProducerActions.Upsert,
                Origin = origin,
                NewSpan = item,
                CameFromFileId = current.Id
            });
        }
    }

    /// <summary>
    /// Processes a single word by comparing current and new values and generating appropriate change requests (update, delete, upsert).
    /// </summary>
    /// <param name="updates">The list to populate with change requests.</param>
    /// <param name="curValue">The current word value.</param>
    /// <param name="newValue">The new word value.</param>
    /// <param name="current">The current file context.</param>
    /// <param name="origin">The origin of the word.</param>
    public void ProcessScalar(
        List<ChangeWordRequest> updates,
        string? curValue,
        string? newValue,
        Files current,
        WordOrigin origin)
    {
        var curEmpty = string.IsNullOrWhiteSpace(curValue);
        var newEmpty = string.IsNullOrWhiteSpace(newValue);

        if (!curEmpty && !newEmpty)
        {
            if (!string.Equals(curValue, newValue, StringComparison.Ordinal))
            {
                updates.Add(new ChangeWordRequest
                {
                    Action = WordProducerActions.Update,
                    Origin = origin,
                    CurrentSpan = curValue,
                    NewSpan = newValue!,
                    CameFromFileId = current.Id
                });
            }
        }
        else if (!curEmpty && newEmpty)
        {
            updates.Add(new ChangeWordRequest
            {
                Action = WordProducerActions.Delete,
                Origin = origin,
                CurrentSpan = curValue,
                NewSpan = null!,
                CameFromFileId = current.Id
            });
        }
        else if (curEmpty && !newEmpty)
        {
            updates.Add(new ChangeWordRequest
            {
                Action = WordProducerActions.Upsert,
                Origin = origin,
                NewSpan = newValue!,
                CameFromFileId = current.Id
            });
        }
    }
}
