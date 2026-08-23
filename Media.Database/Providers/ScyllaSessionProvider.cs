using Cassandra;
using Media.Common.Helpers;

namespace Media.Database.Providers;

/// <summary>
/// Provides Cassandra/Scylla session with self-healing capabilities.
/// Wraps BaseStartup static session management for dependency injection.
/// </summary>
public class ScyllaSessionProvider : IScyllaSessionProvider
{
    /// <summary>
    /// Gets the current Scylla session.
    /// </summary>
    /// <returns>The active ISession for Scylla database operations.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the Scylla session is not initialized.</exception>
    public ISession GetSession()
    {
        return BaseStartup.ScyllaSession;
    }

    /// <summary>
    /// Gets the current session ID for tracking request affinity.
    /// </summary>
    /// <returns>The session ID string.</returns>
    public string GetCurrentSessionId()
    {
        return BaseStartup.GetCurrentRequestSessionId();
    }

    /// <summary>
    /// Attaches the current Scylla session to the current request context.
    /// </summary>
    public void AttachToCurrentRequest()
    {
        BaseStartup.AttachScyllaToCurrentRequest();
    }

    /// <summary>
    /// Attempts to heal a broken Scylla session asynchronously.
    /// </summary>
    /// <param name="brokenSessionId">The ID of the broken session to heal.</param>
    /// <param name="methodName">Optional method name for logging purposes.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task HealSessionAsync(string brokenSessionId, string? methodName = null)
    {
        await BaseStartup.HealScyllaSessionAsync(brokenSessionId, methodName);
    }
}
