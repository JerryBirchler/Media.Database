namespace Media.Database.Models
{
    public record NounExtract
    {
        public Task<IEnumerable<string>> Nouns { get; set; } = null!;
        public WordOrigin Origin { get; set; }
    }
}
