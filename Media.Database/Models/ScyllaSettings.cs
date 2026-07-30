namespace Media.Database.Models
{
    public class ScyllaSettings
    {
        public List<string> ContactPoints { get; set; } = [];
        public int Port { get; set; }
        public List<string> ExternalContactPoints { get; set; } = [];
        public string Keyspace { get; set; } = string.Empty;
        public int MaxBatchsize { get; set; } = 100;
    }
}