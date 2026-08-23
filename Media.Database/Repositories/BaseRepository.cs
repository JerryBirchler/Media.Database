using Cassandra;
using Media.Database.Providers;
using Npgsql;

namespace Media.Database.Repositories
{
    public abstract class BaseRepository
    {
        private readonly IPostgresConnectionProvider _postgresProvider;
        private readonly IScyllaSessionProvider _scyllaProvider;

        protected BaseRepository(IPostgresConnectionProvider postgresProvider, IScyllaSessionProvider scyllaProvider)
        {
            _postgresProvider = postgresProvider ?? throw new ArgumentNullException(nameof(postgresProvider));
            _scyllaProvider = scyllaProvider ?? throw new ArgumentNullException(nameof(scyllaProvider));
        }

        public ISession GetNoSqlConnection()
        {
            return _scyllaProvider.GetSession();
        }

        public NpgsqlConnection GetSqlConnection()
        {
            var connection = new NpgsqlConnection(_postgresProvider.GetConnectionString());
            connection.Open();
            return connection;
        }

        protected IScyllaSessionProvider ScyllaProvider => _scyllaProvider;
    }
}
