using Npgsql;

namespace Media.Database.Helpers;

/// <summary>
/// Extension methods for reading Postgres query results.
/// </summary>
public static class ExtensionMethods
{
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