using Media.Common.Helpers;
using Media.Database.Models;

namespace Media.Database.Helpers;

public static class ExtensionMethods
{
    public static void ProcessList(
        this List<ChangeWordRequest> updates,
        IEnumerable<string>? curList,
        IEnumerable<string>? newList,
        Files current,
        WordOrigin origin)
    {
        var curSet = new HashSet<string>(curList ?? Enumerable.Empty<string>());
        var newSet = new HashSet<string>(newList ?? Enumerable.Empty<string>());

        foreach (var item in curSet.Except(newSet))
        {
            updates.Add(new ChangeWordRequest
            {
                Action = KafkaProducerActions.Delete,
                Origin = origin,
                NewSpan = item,
                CameFromFileId = current.Id
            });
        }

        foreach (var item in newSet.Except(curSet))
        {
            updates.Add(new ChangeWordRequest
            {
                Action = KafkaProducerActions.Upsert,
                Origin = origin,
                NewSpan = item,
                CameFromFileId = current.Id
            });
        }
    }

    public static void ProcessScalar(
        this List<ChangeWordRequest> updates,
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
                    Action = KafkaProducerActions.Update,
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
                Action = KafkaProducerActions.Delete,
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
                Action = KafkaProducerActions.Upsert,
                Origin = origin,
                NewSpan = newValue!,
                CameFromFileId = current.Id
            });
        }
    }

    public static void AddUpsert(this List<BaseWordRequest> list, WordOrigin origin, string word, bool isProperName, Guid cameFromFileId)
    {
        list.Add(new UpsertWordRequest
        {
            Origin = origin,
            Word = word,
            IsProperName = isProperName,
            CameFromFileId = cameFromFileId
        });
    }

    public static void AddDelete(this List<BaseWordRequest> list, WordOrigin origin, string word, bool isProperName, Guid cameFromFileId)
    {
        list.Add(new DeleteWordRequest
        {
            Origin = origin,
            Word = word,
            IsProperName = isProperName,
            CameFromFileId = cameFromFileId
        });
    }

    public static void AddUpsertRange(this List<BaseWordRequest> list, WordOrigin origin, IEnumerable<string> words, Guid cameFromFileId)
    => list.AddUpsertRange(origin, words, null, cameFromFileId);

    public static void AddUpsertRange(this List<BaseWordRequest> list, WordOrigin origin, IEnumerable<string> words, Func<string, bool>? isProperNameResolver, Guid cameFromFileId)
    {
        if (words == null)
            return;

        var resolver = isProperNameResolver ?? (s => s.IsProperName());

        foreach (var word in words)
        {
            list.Add(new UpsertWordRequest
            {
                Origin = origin,
                Word = word,
                IsProperName = resolver(word),
                CameFromFileId = cameFromFileId
            });
        }
    }

    public static void AddDeleteRange(this List<BaseWordRequest> list, WordOrigin origin, IEnumerable<string> words, Guid cameFromFileId)
        => list.AddDeleteRange(origin, words, null, cameFromFileId);

    public static void AddDeleteRange(this List<BaseWordRequest> list, WordOrigin origin, IEnumerable<string> words, Func<string, bool>? isProperNameResolver, Guid cameFromFileId)
    {
        if (words == null)
            return;

        var resolver = isProperNameResolver ?? (Func<string, bool>)(s => s.IsProperName());

        foreach (var word in words)
        {
            list.Add(new DeleteWordRequest
            {
                Origin = origin,
                Word = word,
                IsProperName = resolver(word),
                CameFromFileId = cameFromFileId
            });
        }
    }
}
