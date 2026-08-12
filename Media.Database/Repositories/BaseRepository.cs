using Cassandra;
using Media.Common.Helpers;
using Npgsql;

namespace Media.Database.Repositories
{
    public abstract class BaseRepository
    {
        private readonly string _connectionString = BaseStartup.PostgresConnectionString!;
        private readonly ISession _session = BaseStartup.ScyllaSession!;

        public ISession GetNoSqlConnection()
        {
            return _session;
        }

        public NpgsqlConnection GetSqlConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
