namespace Media.Database.Models;

public class BaseSchemaLookup
{
    public bool? HasFormatter { get; set; } = null;
    public Dictionary<string, string> Names { get; set; } = [];
}
