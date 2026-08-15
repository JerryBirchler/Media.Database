using System.Text.Json.Serialization;

namespace Media.Database.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WordOrigin
{
    Name,
    Keyword,
    FromTitle,
    FromDescription,
    FromEvent,
    FromLocation
}
