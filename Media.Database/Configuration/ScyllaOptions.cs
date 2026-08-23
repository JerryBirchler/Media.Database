namespace Media.Database.Configuration;

public class ScyllaOptions
{
    public const string SectionName = "ScyllaDB";

    public List<string> ContactPoints { get; set; } = new();
    public List<string> ExternalContactPoints { get; set; } = new();
    public int Port { get; set; }
    public string Keyspace { get; set; } = string.Empty;
    public int MaxBatchsize { get; set; } = 100;
}
