namespace Media.Database.Models
{
    public class Words
    {
        public int Id { get; set; }
        public string Word { get; set; }
        public WordOrigin Origin { get; set; }
        public bool IsProperName { get; set; } = false;
        public DateTimeOffset InsertedOn { get; set; }
        public DateTimeOffset? UpdatedOn { get; set; }
        public Guid CameFromFileId { get; set; } 
    }
}
