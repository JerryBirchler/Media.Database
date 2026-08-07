namespace Media.Database.Models;

public record CreateWordRequest
{
    public required string Word { get; set; } 
    public required WordOrigin Origin { get; set; }
    public required bool IsProperName { get; set; }
    public required Guid CameFromFileId { get; set; }
}
