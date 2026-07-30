namespace Media.Database.Models
{
    public class File
    {
        public Guid Id { get; set; }

        public int SourceMachineId { get; set; }

        public string OriginalFilePath { get; set; } = string.Empty;

        public DateTimeOffset InsertedOn { get; set; }

        public DateTimeOffset? UpdatedOn { get; set; }

        public DateTimeOffset? LastFileUpdate { get; set; }

        public bool IsCurrent { get; set; } = true;

        public Metadata? Metadata { get; set; }
    }
}
