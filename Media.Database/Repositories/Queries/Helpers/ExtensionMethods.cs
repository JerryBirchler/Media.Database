using Npgsql;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

namespace Media.Database.Repositories.Queries.Helpers
{
    public static class ExtensionMethods
    {
        public static async Task<NpgsqlCommand> GetCommand(this NpgsqlConnection connection, string query)
        {
            return new NpgsqlCommand(query, connection);
        }

        public static void AddWithValue(this SortedDictionary<string, object> parameters, string name, object value)
        {
            parameters[name.ToUpperInvariant()] = value;
        }

        public static NoSqlCommand GetNoSqlCommand(this Cassandra.ISession session, string parameterizedQuery, int batchSize = 100)
        {
            return new NoSqlCommand(session, parameterizedQuery, batchSize);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved elsewhere or not using Native AOT.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "Reflection deserialization is safe for this application profile.")]
        public static T? GetValueOrDefault<T>(this Cassandra.Row row, string columnName) where T :  class
        {
            if (row.IsNull(columnName))
                return default;

            var value = row.GetValue<string?>(columnName);
            
            if (string.IsNullOrWhiteSpace(value))
                return default;

            return JsonSerializer.Deserialize<T?>(value);
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", Justification = "Types are preserved elsewhere or not using Native AOT.")]
        [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", Justification = "Reflection serialization is safe for this application profile.")]
        public static object ToJsonString<T>(this T model) where T : class
        {
            if (model is DBNull)
                return model;

            if (model == null)
                return string.Empty;
   
            // Converts your C# object into a clean JSON string block
            return JsonSerializer.Serialize(model);
        }

        public static T? ToModelOrDefault<T>(this NpgsqlDataReader reader, string columnName) where T : class
        {
            int ordinal = reader.GetOrdinal(columnName);

            if (reader.IsDBNull(ordinal))
                return null;

            string jsonString = reader.GetString(ordinal);
            return JsonSerializer.Deserialize<T>(jsonString);
        }

        public static object ToNullableValueForSql<T>(this T value) 
        {
            if (value == null) 
                return DBNull.Value;

            return value;
        }

        public static DateTimeOffset? AdjustPrecision(this DateTimeOffset? timestamp)
        {
            if (timestamp is null) 
                return null;

            return (DateTimeOffset?)AdjustPrecision((DateTimeOffset)timestamp);

        }
        public static DateTimeOffset AdjustPrecision(this DateTimeOffset timestamp)
        {
            var dt = timestamp;
            long remainderTicks = dt.Ticks % 10000;
            return dt.AddTicks(-remainderTicks);
        }
    }
}