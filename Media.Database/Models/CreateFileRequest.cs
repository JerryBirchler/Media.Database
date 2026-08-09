namespace Media.Database.Models
{
    public class CreateFileRequest
    {
        public int SourceMachineId { get; set; }

        public string OriginalFilePath { get; set; } = string.Empty;

        public DateTimeOffset? LastFileUpdate { get; set; }

        public Metadata? Metadata { get; set; }
    }
}
