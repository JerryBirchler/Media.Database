namespace Media.Database.Models;

public class SourceMachine
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public DateTimeOffset InsertedOn { get; set; }

    public string? MetaData { get; set; }
}
