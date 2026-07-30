namespace Media.Database.Models
{
    public class UpdateFileRequest
    {
        public DateTimeOffset? LastFileUpdate { get; set; }
        public Metadata? Metadata { get; set; }
    }
}
