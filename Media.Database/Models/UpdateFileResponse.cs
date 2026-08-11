namespace Media.Database.Models;

public record UpdateFileResponse
{
    public Files? File { get; set; }
    public List<ChangeWordRequest> Updates { get; set; } = [];
}
