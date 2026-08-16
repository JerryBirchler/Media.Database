namespace Media.Database.Models;

internal class BaseSchemaLookup
{
    public bool? HasFormatter { get; set; } = null;
    public Dictionary<string, string> Names { get; set; } = [];
}
