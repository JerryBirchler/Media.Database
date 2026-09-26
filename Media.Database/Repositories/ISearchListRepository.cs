using Media.Database.Models;

namespace Media.Database.Repositories;

/// <summary>
/// Saved search lists (API-115), assembled across both stores: Postgres for identity, owner, type
/// and timestamps; Scylla for the name and the lines, encrypted.
///
/// Every method takes the owner explicitly and every statement behind them is scoped by it.
/// Knowing a uuid grants nothing -- that is the point, not a detail. A list belonging to somebody
/// else is indistinguishable from one that does not exist, because a distinguishable "forbidden"
/// would itself confirm the uuid is real.
/// </summary>
public interface ISearchListRepository
{
    /// <summary>
    /// Creates a list. Postgres issues the identity and the uuid, then the name and lines are
    /// encrypted and written to Scylla under the borrowed id.
    /// </summary>
    Task<SearchList?> AddAsync(OwnerScope scope, int ownerId, SearchListType listType, string name, IReadOnlyList<SearchListLine> lines);

    /// <summary>
    /// Reads one list, fully assembled and decrypted, or null when it does not exist or does not
    /// belong to <paramref name="ownerId"/> -- the two cases are deliberately not distinguished.
    /// </summary>
    Task<SearchList?> GetAsync(OwnerScope scope, int ownerId, Guid uuid);

    /// <summary>
    /// Reads every list an owner has, optionally narrowed to one kind. One Postgres index scan for
    /// the skeletons and one Scylla partition read for the names, rather than a round trip each.
    /// </summary>
    Task<IReadOnlyList<SearchList>> GetAllAsync(OwnerScope scope, int ownerId, SearchListType? listType = null);

    /// <summary>
    /// Replaces a list's name and lines. Returns null when the list is not the owner's, in which
    /// case nothing is written to either store.
    /// </summary>
    Task<SearchList?> UpdateAsync(OwnerScope scope, int ownerId, Guid uuid, SearchListType listType, string name, IReadOnlyList<SearchListLine> lines);

    /// <summary>
    /// Removes a list from both stores. False when it was not the owner's, in which case nothing
    /// was removed.
    /// </summary>
    Task<bool> DeleteAsync(OwnerScope scope, int ownerId, Guid uuid);
}
