namespace Media.Database.Models;

public record KafkaSettings
{
    public required Uri BaseUrl { get; set; }
    public required int Port { get; set; }
    public required string ClusterId { get; set; }
}