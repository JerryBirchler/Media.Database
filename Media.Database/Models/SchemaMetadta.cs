namespace Media.Database.Models;

internal class SchemaMetadata
{
    public Func<string, string>? FormatDelegate { get; set; }
    public Type ParentType { get; set; } = null!;
    public BaseSchemaLookup ParentValue { get; set; } = null!;
    public Type ChildType { get; set; } = null!;
    public BaseSchemaLookup ChildValue { get; set; } = null!;
    public object? SubFields { get; set; }
}
