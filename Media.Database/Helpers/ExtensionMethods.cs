using Media.Database.Mappers;
using Media.Database.Models;

namespace Media.Database.Helpers;

public static class ExtensionMethods
{
    public static void ProcessList(
        this List<ChangeWordRequest> updates,
        IEnumerable<string>? curList,
        IEnumerable<string>? newList,
        Files current,
        WordOrigin origin,
        IMapChangeWordRequests mapper)
    {
        mapper.ProcessList(updates, curList, newList, current, origin);
    }

    public static void ProcessScalar(
        this List<ChangeWordRequest> updates,
        string? curValue,
        string? newValue,
        Files current,
        WordOrigin origin,
        IMapChangeWordRequests mapper)
    {
        mapper.ProcessScalar(updates, curValue, newValue, current, origin);
    }
}