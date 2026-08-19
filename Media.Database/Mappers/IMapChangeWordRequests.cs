using Media.Database.Models;

namespace Media.Database.Mappers;

public interface IMapChangeWordRequests
{
    void ProcessList(
        List<ChangeWordRequest> updates,
        IEnumerable<string>? curList,
        IEnumerable<string>? newList,
        Files current,
        WordOrigin origin);
    void ProcessScalar(
        List<ChangeWordRequest> updates,
        string? curValue,
        string? newValue,
        Files current,
        WordOrigin origin);
}