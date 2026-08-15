using Cassandra;
using Media.Database.Models;

namespace Media.Database.Repositories;

public static class BaseStartup
{
    public static string? PostgresConnectionString { get; set; }
    public static ISession? ScyllaSession { get; set; }
    public static ScyllaSettings? ScyllaSettings { get; set; }
}
