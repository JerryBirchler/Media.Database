using Cassandra;
using Media.Database.Helpers;
using Media.Database.Models;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Media.Database.Repositories
{
    public class BaseRepository(IConfiguration configuration)
    {
        private readonly string _connectionString = configuration.GetConnectionString("PostgresConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'PostgresConnection' not found.");

        internal readonly ScyllaSettings _scyllaSettings = configuration.GetSection("ScyllaDB").Get<ScyllaSettings>()
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'ScyllaDB' not found.");

        private readonly Lock _sessionLock = new();
        private static Cassandra.ISession? _session = null;

        public Cassandra.ISession GetNoSqlConnection()
        {
            if (_session != null)
            {
                return _session; 
            }

            lock (_sessionLock)
            {
                if (_session == null)
                {
                    var addressTranslator = new DockerPortTranslator(_scyllaSettings);

                    var cluster = Cluster.Builder()
                        .AddContactPoints(_scyllaSettings.ContactPoints[0].ToString())
                        .WithPort(_scyllaSettings.Port)
                        .WithAddressTranslator(addressTranslator)
                        .Build();

                    _session = cluster.Connect(_scyllaSettings.Keyspace);
                }

                return _session;
            }
        }

        public NpgsqlConnection GetSqlConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
