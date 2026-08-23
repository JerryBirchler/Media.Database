using Cassandra;

namespace Media.Database.Providers;

/// <summary>
/// Provides Cassandra/Scylla session for NoSQL database operations with self-healing capabilities.
/// </summary>
public interface IScyllaSessionProvider
{
    /// <summary>
    /// Gets the current Scylla session.
    /// </summary>
    /// <returns>The active ISession for Scylla database operations.</returns>
    ISession GetSession();

    /// <summary>
    /// Gets the current session ID for tracking request affinity.
    /// </summary>
    /// <returns>The session ID string.</returns>
    string GetCurrentSessionId();

    /// <summary>
    /// Attaches the current Scylla session to the current request context.
    /// </summary>
    void AttachToCurrentRequest();

    /// <summary>
    /// Attempts to heal a broken Scylla session asynchronously.
    /// </summary>
    /// <param name="brokenSessionId">The ID of the broken session to heal.</param>
    /// <param name="methodName">Optional method name for logging purposes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task HealSessionAsync(string brokenSessionId, string? methodName = null);
}
