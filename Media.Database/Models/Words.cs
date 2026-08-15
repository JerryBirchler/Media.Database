using System.Text.Json.Serialization;

namespace Media.Database.Models;

public class Words
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("word")]
    public string Word { get; set; } = string.Empty;

    [JsonPropertyName("origin")]
    public WordOrigin Origin { get; set; }

    [JsonPropertyName("isProperName")]
    public bool IsProperName { get; set; } = false;

    [JsonPropertyName("insertedOn")]
    public DateTimeOffset InsertedOn { get; set; }

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset? UpdatedOn { get; set; }

    [JsonPropertyName("cameFromFileId")]
    public Guid CameFromFileId { get; set; }
}
