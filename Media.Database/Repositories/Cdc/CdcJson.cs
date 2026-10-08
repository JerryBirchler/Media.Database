using System.Text.Json;

namespace Media.Database.Repositories.Cdc;

/// <summary>
/// Reads nullable columns out of a Debezium change record's <c>after</c> image, where a SQL NULL
/// arrives as JSON null. Shared by the group-joining handlers (SCHEMA-35); the older handlers keep
/// their own private copies.
/// </summary>
internal static class CdcJson
{
    public static int? GetNullableInt32(this JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetInt32();
    }

    public static DateTimeOffset? GetNullableDateTimeOffset(this JsonElement element, string propertyName)
    {
        var property = element.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetDateTimeOffset();
    }

    /// <summary>The integer id named <paramref name="propertyName"/> in a change record's key.</summary>
    public static int GetKeyId(string key, string propertyName)
    {
        using var document = JsonDocument.Parse(key);
        return document.RootElement.GetProperty(propertyName).GetInt32();
    }
}
