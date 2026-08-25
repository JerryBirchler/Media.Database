namespace Media.Database.Repositories.Schemas;

/// <summary>
/// Sentinel <c>TChild</c> type for a <see cref="BaseSchema{TParent, TChild}"/> whose field names
/// are used as-is, with no child-schema lookup or formatting applied.
/// </summary>
public sealed class NoSubFields : ISchema { }