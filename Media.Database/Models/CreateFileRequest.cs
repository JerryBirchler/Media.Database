namespace Media.Database.Models;

public record CreateFileRequest
{
    public required int SourceMachineId { get; set; }

    public required string OriginalFilePath { get; set; } 

    public DateTimeOffset? LastFileUpdate { get; set; }

    public Metadata? Metadata { get; set; }
}
