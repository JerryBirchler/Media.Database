namespace Media.Database.Models
{
    internal record NounExtract
    {
        public Task<IEnumerable<string>> Nouns { get; set; } = null!;
        public WordOrigin Origin { get; set; }
    }
}
