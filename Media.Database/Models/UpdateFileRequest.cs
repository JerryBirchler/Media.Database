namespace Media.Database.Models
{
    public record UpdateFileRequest
    {
        public DateTimeOffset? LastFileUpdate { get; set; }
        public Metadata? Metadata { get; set; }
    }
}
