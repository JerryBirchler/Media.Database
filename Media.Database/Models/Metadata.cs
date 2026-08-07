namespace Media.Database.Models;

public class Metadata
{
    public SortedSet<string>? KeyWords { get; set; } = null;
    public SortedSet<string>? Names { get; set; } = null;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Event { get; set; }
    public string? Location { get; set; }        
}
