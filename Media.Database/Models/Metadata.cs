using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace Media.Database.Models;

public class Metadata
{
    [JsonPropertyName("keyWords")]
    [FromHeader(Name = "keyWords")]
    public SortedSet<string>? KeyWords { get; set; } = null;

    [JsonPropertyName("names")]
    [FromHeader(Name = "names")]
    public SortedSet<string>? Names { get; set; } = null;

    [JsonPropertyName("title")]
    [FromHeader(Name = "title")]
    public string? Title { get; set; }

    [JsonPropertyName("description")]
    [FromHeader(Name = "description")]
    public string? Description { get; set; }

    [JsonPropertyName("event")]
    [FromHeader(Name = "event")]
    public string? Event { get; set; }

    [JsonPropertyName("location")]
    [FromHeader(Name = "location")]
    public string? Location { get; set; }
}
