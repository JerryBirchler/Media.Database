using Cassandra;
using Media.Common.Providers;

namespace Media.Database.Repositories
{
    /// <summary>
    /// Base class for repositories, providing access to the Scylla/Cassandra session for
    /// implementations that also need NoSQL storage. PostgreSQL access is provided separately
    /// via <see cref="ISqlQueryExecutor"/>.
    /// </summary>
    public abstract class BaseRepository
    {
        private readonly IScyllaSessionProvider? _scyllaProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository"/> class for a repository
        /// that requires Scylla/Cassandra access.
        /// </summary>
        /// <param name="scyllaProvider">The Scylla session provider.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="scyllaProvider"/> is null.</exception>
        protected BaseRepository(IScyllaSessionProvider scyllaProvider)
        {
            _scyllaProvider = scyllaProvider ?? throw new ArgumentNullException(nameof(scyllaProvider));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseRepository"/> class for a repository
        /// that does not require Scylla/Cassandra access.
        /// </summary>
        protected BaseRepository()
        {
            _scyllaProvider = null;
        }

        /// <summary>
        /// Gets the active Scylla/Cassandra session.
        /// </summary>
        /// <returns>The current <see cref="ISession"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown when this repository was not constructed with a Scylla session provider.</exception>
        public ISession GetNoSqlConnection()
        {
            return _scyllaProvider?.GetSession() ?? throw new InvalidOperationException("Scylla provider not initialized");
        }

        /// <summary>
        /// Gets the Scylla session provider, or null if this repository was not constructed with one.
        /// </summary>
        protected IScyllaSessionProvider? ScyllaProvider => _scyllaProvider;
    }
}
