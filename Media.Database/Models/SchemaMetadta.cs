namespace Media.Database.Models;

internal class SchemaMetadata
{
    public Func<string, string>? FormatDelegate { get; set; }
    public Type T1Type { get; set; } = null!;
    public BaseSchemaLookup T1Value { get; set; } = null!;
    public Type T2Type { get; set; } = null!;
    public BaseSchemaLookup T2Value { get; set; } = null!;
    public object? SubFields { get; set; }
}
