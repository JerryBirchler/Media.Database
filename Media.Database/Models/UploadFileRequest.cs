using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

public record UploadFileRequest
{
    [JsonPropertyName("sourceMachineId")]
    [FromHeader(Name = "sourceMachineId")]
    public required int SourceMachineId { get; set; }

    [JsonPropertyName("originalFilePath")]
    [FromHeader(Name = "originalFilePath")]
    public required string OriginalFilePath { get; set; }

    [JsonPropertyName("lastFileUpdate")]
    [FromHeader(Name = "lastFileUpdate")]
    public DateTimeOffset? LastFileUpdate { get; set; }

    [JsonPropertyName("metadata")]
    [FromHeader(Name = "metadata")]
    public Metadata? Metadata { get; set; }
}
