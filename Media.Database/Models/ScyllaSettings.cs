namespace Media.Database.Models;

public record ScyllaSettings
{
    public required List<Uri> ContactPoints { get; set; }
    public required List<Uri> ExternalContactPoints { get; set; }
    public required int Port { get; set; }
    public required string Keyspace { get; set; }
    public required int MaxBatchsize { get; set; }
}
