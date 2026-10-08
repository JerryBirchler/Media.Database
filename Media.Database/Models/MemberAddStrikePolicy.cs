namespace Media.Database.Models;

/// <summary>
/// How a miss -- adding a member by last name and email who is not there -- changes an admin's
/// strikes (MEDIA-8). Pure, so the rule is testable without a database and the repository only
/// stores what it decides.
///
/// <list type="bullet">
///   <item>A miss after the window has ended (or with no strikes) starts over at 1, with a window of
///   the base length <c>n</c> from now.</item>
///   <item>A miss inside the window adds one and doubles the window, from now: the k-th strike has
///   a window of <c>2^(k-1) * n</c> -- n, 2n, 4n.</item>
///   <item>The <see cref="LockingStrike"/>-th strike locks the account. A lock is never cleared
///   here -- only re-registration clears it -- so a lock already set keeps its moment.</item>
/// </list>
/// </summary>
public static class MemberAddStrikePolicy
{
    /// <summary>The strike that locks the account.</summary>
    public const int LockingStrike = 3;

    /// <summary>
    /// Doubling stops growing past this many doublings (1024 x the base window), so a long run of
    /// strikes can never overflow a date.
    /// </summary>
    public const int MaxDoublings = 10;

    /// <summary>
    /// The strikes after one more miss.
    /// </summary>
    /// <param name="current">The strikes now, or null when the person has none recorded.</param>
    /// <param name="personId">The person missing.</param>
    /// <param name="now">The moment of the miss.</param>
    /// <param name="baseWindow">The configured base window, <c>n</c>. Must be positive.</param>
    public static PersonMemberAddStrikes AfterMiss(PersonMemberAddStrikes? current, int personId, DateTimeOffset now, TimeSpan baseWindow)
    {
        if (baseWindow <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(baseWindow), baseWindow, "The strike window must be positive.");

        var stillInWindow = current is { StrikeCount: > 0, WindowEndsOn: { } endsOn } && endsOn > now;
        var count = stillInWindow ? current!.StrikeCount + 1 : 1;
        var doublings = Math.Min(count - 1, MaxDoublings);
        var lockedOn = current?.LockedOn ?? (count >= LockingStrike ? now : null);

        return new PersonMemberAddStrikes
        {
            PersonId = personId,
            StrikeCount = count,
            WindowEndsOn = now + baseWindow * (1 << doublings),
            LockedOn = lockedOn,
            InsertedOn = current?.InsertedOn ?? now,
            UpdatedOn = now
        };
    }
}
