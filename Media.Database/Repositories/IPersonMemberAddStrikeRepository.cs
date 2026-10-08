using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// The misses an admin has made adding members by last name and email (SCHEMA-35, MEDIA-8). The
/// rule for a miss is <see cref="MemberAddStrikePolicy"/>; this stores it.
/// </summary>
public interface IPersonMemberAddStrikeRepository
{
    /// <summary>A person's strikes, or null when they have never missed.</summary>
    Task<PersonMemberAddStrikes?> GetAsync(int personId);

    /// <summary>
    /// Records a miss for <paramref name="personId"/> with the configured base window
    /// <paramref name="baseWindow"/> (n): after the window has ended it starts over at 1 with window
    /// n; inside it, it adds one and doubles the window from now (n, 2n, 4n); the third strike locks.
    /// Safe against concurrent misses -- each one counts once. Returns the strikes after the miss.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="baseWindow"/> is not positive.</exception>
    Task<PersonMemberAddStrikes> RecordMissAsync(int personId, TimeSpan baseWindow);

    /// <summary>
    /// A successful registration: zeroes the count and clears the lock. Returns the cleared row, or
    /// null when the person had none.
    /// </summary>
    Task<PersonMemberAddStrikes?> ClearAsync(int personId);

    /// <summary>
    /// The reset sweep: zeroes every count whose window has ended, leaving locks as they are.
    /// Returns how many were reset.
    /// </summary>
    Task<int> ResetEndedWindowsAsync();
}
