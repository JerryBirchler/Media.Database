using Cassandra;
using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Microsoft.Extensions.Logging;

namespace Media.Database.Repositories
{
    /// <summary>
    /// Base class for repositories, providing access to the Scylla/Cassandra session for
    /// implementations that also need CQL storage. PostgreSQL access is provided separately
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
        public ISession GetCqlConnection()
        {
            return _scyllaProvider?.GetSession() ?? throw new InvalidOperationException("Scylla provider not initialized");
        }

        /// <summary>
        /// Gets the Scylla session provider, or null if this repository was not constructed with one.
        /// </summary>
        protected IScyllaSessionProvider? ScyllaProvider => _scyllaProvider;

        /// <summary>
        /// Cassandra driver exceptions that indicate the cluster/session is unreachable, as opposed to a single
        /// query timing out against an otherwise healthy cluster. Only these warrant rebuilding the session.
        /// </summary>
        /// <param name="ex">The exception to check.</param>
        /// <returns>True if the exception indicates a connectivity issue with the Scylla cluster; otherwise, false.</returns>
        protected static bool IsScyllaConnectivityException(Exception ex) =>
            ex is NoHostAvailableException or UnavailableException or OperationTimedOutException;

        /// <summary>
        /// Attempts to heal the Scylla session without letting a healing failure (e.g. a busy self-heal lock)
        /// mask the original exception that triggered the heal attempt.
        /// </summary>
        /// <typeparam name="T">The calling repository's own type, so the heal-failure log line is tagged with it.</typeparam>
        /// <param name="logger">The calling repository's fluent logger.</param>
        /// <param name="methodName">The name of the method that triggered the heal attempt.</param>
        protected async Task TryHealScyllaSessionAsync<T>(FluentLogger<T> logger, string methodName)
        {
            try
            {
                await ScyllaProvider!.HealSessionAsync(ScyllaProvider.GetCurrentSessionId(), methodName);
            }
            catch (Exception healEx)
            {
                logger.WithCaller().LogError(healEx, "Scylla session heal attempt failed in {OriginatingMethod}", methodName);
            }
        }
    }
}
