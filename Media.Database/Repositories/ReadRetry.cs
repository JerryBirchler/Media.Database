namespace Media.Database.Repositories;

/// <summary>
/// Tries a read again when the connection under it failed, rather than failing the request. A pooled
/// connection can be dropped while it sits idle -- a network change, a load balancer's timeout, a
/// database failing over -- and the first statement sent on it then fails although the database is
/// fine. The next attempt, on a fresh connection, succeeds.
/// </summary>
/// <remarks>
/// Only plain reads (statements that begin with <c>SELECT</c>) are tried again, and only for a
/// failure of the connection itself. A write is never repeated: the connection can fail after the
/// database applied it, and applying it twice is worse than reporting the failure. A statement that
/// fails for any other reason -- bad SQL, a constraint, a timeout on a healthy connection that the
/// caller should see -- fails at once.
/// </remarks>
/// <param name="delays">
/// The wait before each further attempt; their count is how many further attempts there are.
/// </param>
public sealed class ReadRetry(IReadOnlyList<TimeSpan> delays)
{
    /// <summary>
    /// Three more attempts over about two and a half seconds: long enough for a driver that marked
    /// its only host down to reconnect, short enough that a database that is really gone still
    /// fails the request promptly.
    /// </summary>
    public static ReadRetry Default { get; } = new([TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(750), TimeSpan.FromMilliseconds(1500)]);

    /// <summary>The wait before each further attempt.</summary>
    public IReadOnlyList<TimeSpan> Delays => delays;

    /// <summary>Whether a statement is a plain read, and so safe to send again.</summary>
    public static bool IsRead(string statement) =>
        statement.TrimStart().StartsWith("SELECT", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Runs <paramref name="attempt"/>; if <paramref name="statement"/> is a read and the attempt
    /// fails with a connection failure, runs <paramref name="beforeRetry"/>, waits, and tries again,
    /// up to the number of delays. The last failure is the one that surfaces.
    /// </summary>
    /// <param name="statement">The SQL or CQL being run: decides whether it may be tried again.</param>
    /// <param name="attempt">One attempt at running it, on whatever connection it gets.</param>
    /// <param name="isConnectionFailure">Whether a failure was the connection's, not the statement's.</param>
    /// <param name="beforeRetry">Anything to do before trying again, such as clearing a connection pool.</param>
    public async Task<T> RunAsync<T>(string statement, Func<Task<T>> attempt, Func<Exception, bool> isConnectionFailure, Action? beforeRetry = null)
    {
        if (!IsRead(statement))
            return await attempt();

        for (var tried = 0; ; tried++)
        {
            try
            {
                return await attempt();
            }
            catch (Exception exception) when (tried < delays.Count && isConnectionFailure(exception))
            {
                beforeRetry?.Invoke();
                await Task.Delay(delays[tried]);
            }
        }
    }
}
