namespace Media.Database.Models;

/// <summary>
/// Internal lookup information for base schema metadata.
/// </summary>
internal class BaseSchemaLookup
{
    /// <summary>
    /// Gets or sets a value indicating whether the schema has a custom formatter.
    /// </summary>
    public bool? HasFormatter { get; set; } = null;

    /// <summary>
    /// Gets or sets the dictionary of schema field names.
    /// </summary>
    public Dictionary<string, string> Names { get; set; } = [];
}
