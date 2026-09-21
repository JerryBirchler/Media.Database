namespace Media.Database.Models;

/// <summary>
/// Internal metadata describing a schema type pair, cached once per closed generic
/// <c>BaseSchema&lt;TParent, TChild&gt;</c> to avoid repeated reflection.
/// </summary>
internal class SchemaMetadata
{
    /// <summary>
    /// Gets or sets the delegate used to format field names, if the parent type defines one.
    /// </summary>
    public Func<string, string>? FormatDelegate { get; set; }

    /// <summary>
    /// Gets or sets the parent schema type.
    /// </summary>
    public Type ParentType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cached lookup for the parent schema type's field names.
    /// </summary>
    public BaseSchemaLookup ParentValue { get; set; } = null!;

    /// <summary>
    /// Gets or sets the child schema type.
    /// </summary>
    public Type ChildType { get; set; } = null!;

    /// <summary>
    /// Gets or sets the cached lookup for the child schema type's field names.
    /// </summary>
    public BaseSchemaLookup ChildValue { get; set; } = null!;

    /// <summary>
    /// Gets or sets the instantiated child schema, used to resolve sub-field names via reflection.
    /// </summary>
    public object? SubFields { get; set; }
}
