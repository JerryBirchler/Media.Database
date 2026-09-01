using Media.Database.Mappers;
using Media.Database.Models;
using Npgsql;

namespace Media.Database.Helpers;

/// <summary>
/// Extension methods for processing word change requests.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Processes a list of words by comparing current and new lists and adding change requests.
    /// </summary>
    /// <param name="updates">The list of change requests to populate.</param>
    /// <param name="curList">The current list of words.</param>
    /// <param name="newList">The new list of words.</param>
    /// <param name="current">The current file context.</param>
    /// <param name="origin">The origin of the words.</param>
    /// <param name="mapper">The mapper to use for processing.</param>
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

    /// <summary>
    /// Processes a single word by comparing current and new values and adding change requests.
    /// </summary>
    /// <param name="updates">The list of change requests to populate.</param>
    /// <param name="curValue">The current word value.</param>
    /// <param name="newValue">The new word value.</param>
    /// <param name="current">The current file context.</param>
    /// <param name="origin">The origin of the word.</param>
    /// <param name="mapper">The mapper to use for processing.</param>
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

    public static string GetString(this NpgsqlDataReader reader, string columName)
    {
        return reader.GetString(reader.GetOrdinal(columName));
    }

    public static int GetInt32(this NpgsqlDataReader reader, string columName)
    {
        return reader.GetInt32(reader.GetOrdinal(columName));
    }

    public static Guid GetGuid(this NpgsqlDataReader reader, string columName)
    {
        return reader.GetGuid(reader.GetOrdinal(columName));
    }

    public static T GetFieldValue<T>(this NpgsqlDataReader reader, string columName)
    {
        return reader.GetFieldValue<T>(reader.GetOrdinal(columName));
    }
}