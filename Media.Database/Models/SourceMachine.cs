namespace Media.Database.Models;

/// <summary>
/// Represents a machine that originates files tracked by the media database.
/// </summary>
public class SourceMachine
{
    /// <summary>
    /// Gets or sets the unique identifier for the source machine.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the source machine.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the timestamp when the source machine record was inserted.
    /// </summary>
    public DateTimeOffset InsertedOn { get; set; }

    /// <summary>
    /// Gets or sets additional metadata about the source machine.
    /// </summary>
    public string? MetaData { get; set; }
}
