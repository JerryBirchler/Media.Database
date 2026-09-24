using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Reads and writes one-time-password attempt counters (MEDIA-40). Scylla only.
/// </summary>
public interface IOtpAttemptRepository
{
    /// <summary>Gets the counter for <paramref name="nonceId"/>, or null when it has had no tries.</summary>
    Task<OtpAttempt?> GetAsync(string nonceId);

    /// <summary>
    /// Writes (or overwrites) a counter, expiring it after <paramref name="timeToLive"/> -- the
    /// time left in the verification window -- so it never outlives the codes it guards.
    /// </summary>
    Task SaveAsync(OtpAttempt attempt, TimeSpan timeToLive);
}
