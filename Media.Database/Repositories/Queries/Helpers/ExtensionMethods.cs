using Npgsql;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Media.Database.Repositories.Queries.Helpers;

/// <summary>
/// General-purpose helpers for building NoSQL commands and mapping between SQL/CQL rows and models.
/// </summary>
public static class ExtensionMethods
{
    /// <summary>
    /// Adds or replaces a named parameter, keyed case-insensitively by its upper-invariant name.
    /// </summary>
    /// <param name="parameters">The parameter dictionary to add to.</param>
    /// <param name="name">The parameter name.</param>
    /// <param name="value">The parameter value.</param>
    public static void AddWithValue(this SortedDictionary<string, object> parameters, string name, object value)
    {
        parameters[name.ToUpperInvariant()] = value;
    }

    /// <summary>
    /// Creates a <see cref="NoSqlCommand"/> bound to the given session and parameterized CQL query.
    /// </summary>
    /// <param name="session">The Cassandra/Scylla session to execute against.</param>
    /// <param name="parameterizedQuery">The CQL query text, with named (<c>@name</c>) parameters.</param>
    /// <param name="batchSize">The number of statements to accumulate before flushing a batch.</param>
    /// <returns>A new <see cref="NoSqlCommand"/>.</returns>
    public static NoSqlCommand GetNoSqlCommand(this Cassandra.ISession session, string parameterizedQuery, int batchSize = 100)
    {
        return new NoSqlCommand(session, parameterizedQuery, batchSize);
    }

    /// <summary>
    /// Deserializes a JSON column of a Cassandra/Scylla row, returning null if the column is null or blank.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="row">The row to read from.</param>
    /// <param name="columnName">The name of the JSON column.</param>
    /// <returns>The deserialized value, or null.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved elsewhere or not using Native AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "Reflection deserialization is safe for this application profile.")]
    public static T? GetValueOrDefault<T>(this Cassandra.Row row, string columnName) where T : class
    {
        if (row.IsNull(columnName))
            return default;

        var value = row.GetValue<string?>(columnName);

        if (string.IsNullOrWhiteSpace(value))
            return default;

        return JsonSerializer.Deserialize<T?>(value);
    }

    /// <summary>
    /// Serializes <paramref name="model"/> to a JSON string suitable for a SQL/CQL parameter value.
    /// </summary>
    /// <typeparam name="T">The type of the model.</typeparam>
    /// <param name="model">The model to serialize.</param>
    /// <returns>The JSON string, <see cref="string.Empty"/> if <paramref name="model"/> is null, or <see cref="DBNull"/> unchanged.</returns>
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved elsewhere or not using Native AOT.")]
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "Reflection serialization is safe for this application profile.")]
    public static object ToJsonString<T>(this T model) where T : class
    {
        if (model is DBNull)
            return model;

        if (model == null)
            return string.Empty;

        return JsonSerializer.Serialize(model);
    }

    /// <summary>
    /// Deserializes a JSON column of an <see cref="NpgsqlDataReader"/> row, returning null if the column is DB null.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to.</typeparam>
    /// <param name="reader">The reader positioned on the row to read from.</param>
    /// <param name="columnName">The name of the JSON column.</param>
    /// <returns>The deserialized value, or null.</returns>
    public static T? ToModelOrDefault<T>(this NpgsqlDataReader reader, string columnName) where T : class
    {
        int ordinal = reader.GetOrdinal(columnName);

        if (reader.IsDBNull(ordinal))
            return null;

        string jsonString = reader.GetString(ordinal);
        return JsonSerializer.Deserialize<T>(jsonString);
    }

    /// <summary>
    /// Converts a possibly-null value to a form suitable for a SQL parameter, mapping null to <see cref="DBNull.Value"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to convert.</param>
    /// <returns><paramref name="value"/> unchanged, or <see cref="DBNull.Value"/> if it was null.</returns>
    public static object ToNullableValueForSql<T>(this T value)
    {
        if (value == null)
            return DBNull.Value;

        return value;
    }

    /// <summary>
    /// Truncates a timestamp to the precision PostgreSQL preserves round-trip, if it has a value.
    /// </summary>
    /// <param name="timestamp">The timestamp to adjust.</param>
    /// <returns>The adjusted timestamp, or null.</returns>
    public static DateTimeOffset? AdjustPrecision(this DateTimeOffset? timestamp)
    {
        if (timestamp is null)
            return null;

        return (DateTimeOffset?)AdjustPrecision((DateTimeOffset)timestamp);

    }

    /// <summary>
    /// Truncates a timestamp to the precision PostgreSQL preserves round-trip.
    /// </summary>
    /// <param name="timestamp">The timestamp to adjust.</param>
    /// <returns>The adjusted timestamp.</returns>
    public static DateTimeOffset AdjustPrecision(this DateTimeOffset timestamp)
    {
        var dt = timestamp;
        long remainderTicks = dt.Ticks % 10000;
        return dt.AddTicks(-remainderTicks);
    }
}