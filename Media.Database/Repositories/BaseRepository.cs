using Cassandra;
using Media.Common.Helpers;
using Npgsql;

namespace Media.Database.Repositories
{
    public abstract class BaseRepository
    {
        private readonly string _connectionString = BaseStartup.PostgresConnectionString!;

        public ISession GetNoSqlConnection()
        {
            return BaseStartup.ScyllaSession;
        }

        public NpgsqlConnection GetSqlConnection()
        {
            var connection = new NpgsqlConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
