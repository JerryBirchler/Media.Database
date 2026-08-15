using System.Text.Json.Serialization;

namespace Media.Database.Models;

public class WordFiles
{
    [JsonPropertyName("origin")]
    public WordOrigin Origin { get; set; }

    [JsonPropertyName("WordId")]
    public int WordId { get; set; }

    [JsonPropertyName("fileId")]
    public Guid FileId { get; set; }
}
