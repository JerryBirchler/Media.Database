using System.Text.Json.Serialization;

namespace Media.Database.Models;

public class Files
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("sourceMachineId")]
    public int SourceMachineId { get; set; }

    [JsonPropertyName("originFilePath")]
    public string OriginalFilePath { get; set; } = string.Empty;

    [JsonPropertyName("insertOn")]
    public DateTimeOffset InsertedOn { get; set; }

    [JsonPropertyName("updatedOn")]
    public DateTimeOffset? UpdatedOn { get; set; }

    [JsonPropertyName("lastFileUpdate")]
    public DateTimeOffset? LastFileUpdate { get; set; }

    [JsonPropertyName("isCurrent")]
    public bool IsCurrent { get; set; } = true;

    [JsonPropertyName("metadata")]
    public Metadata? Metadata { get; set; }
}
