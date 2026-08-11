using System.Text.Json.Serialization;

namespace Media.Database.Models;

public class ViewWordFiles
{
    [JsonPropertyName("origin")]
    public WordOrigin Origin { get; set; }

    [JsonPropertyName("wordId")]
    public int WordId { get; set; }

    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("fileId")]
    public Guid FileId { get; set; }

    [JsonPropertyName("isCurrent")]
    public bool? IsCurrent { get; set; }

    [JsonPropertyName("isProperName")]
    public bool? IsProperName { get; set; }
}
