namespace Media.Database.Models
{
    /// <summary>
    /// Request model for creating a new file record.
    /// </summary>
    public class CreateFileRequest
    {
        /// <summary>
        /// Gets or sets the source machine identifier.
        /// </summary>
        public int SourceMachineId { get; set; }

        /// <summary>
        /// Gets or sets the original file path.
        /// </summary>
        public string OriginalFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the timestamp of the last file update.
        /// </summary>
        public DateTimeOffset? LastFileUpdate { get; set; }

        /// <summary>
        /// Gets or sets the file metadata.
        /// </summary>
        public Metadata? Metadata { get; set; }
    }
}
