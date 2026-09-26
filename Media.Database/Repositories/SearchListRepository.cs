using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using System.Text.Json;
#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="ISearchListRepository"/>
public class SearchListRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    ISearchListCipher cipher,
    ILogger<SearchListRepository> logger) : ISearchListRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly ISearchListCipher _cipher = cipher;
    private readonly FluentLogger<SearchListRepository> _logger = logger.Initializer();

    // Nothing a user wrote is ever logged here -- not a name, not a line, not a decrypted payload.
    // Encrypting them at rest and then printing them into a log file would be theater.

    public async Task<SearchList?> AddAsync(
        OwnerScope scope, int ownerId, SearchListType listType, string name, SearchListPayload payload)
    {
        try
        {
            var skeleton = await _sqlExecutor.QuerySingleAsync(
                scope == OwnerScope.Person ? QuerySearchLists.AddPersonSql : QuerySearchLists.AddGroupSql,
                p =>
                {
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                    p.AddWithValue(pn.ListType, (int)listType);
                },
                reader => scope == OwnerScope.Person ? reader.ToPersonSearchList() : reader.ToGroupSearchList());

            if (skeleton is null)
                return null;

            await WriteContentAsync(scope, ownerId, skeleton.Id, name, payload);

            skeleton.Name = name;
            skeleton.Lines = payload.Lines;
            skeleton.References = payload.References;
            return skeleton;
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex,
                "AddAsync failed for a search list. Scope: [{Scope}] OwnerId: [{OwnerId}]", scope, ownerId);
            throw;
        }
    }

    public async Task<SearchList?> GetAsync(OwnerScope scope, int ownerId, Guid uuid)
    {
        try
        {
            var skeleton = await GetSkeletonAsync(scope, ownerId, uuid);

            if (skeleton is null)
                return null;

            var content = await _cqlExecutor.QuerySingleAsync(
                scope == OwnerScope.Person ? QuerySearchListsCql.GetPersonSql : QuerySearchListsCql.GetGroupSql,
                p =>
                {
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonSearchListId : pn.GroupSearchListId, skeleton.Id);
                },
                row => scope == OwnerScope.Person
                    ? row.ToPersonSearchListContent()
                    : row.ToGroupSearchListContent());

            Hydrate(skeleton, content);
            return skeleton;
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex,
                "GetAsync failed for a search list. Scope: [{Scope}] OwnerId: [{OwnerId}] Uuid: [{Uuid}]",
                scope, ownerId, uuid);
            throw;
        }
    }

    public async Task<IReadOnlyList<SearchList>> GetAllAsync(
        OwnerScope scope, int ownerId, SearchListType? listType = null)
    {
        try
        {
            var skeletons = await _sqlExecutor.QueryManyAsync(
                scope == OwnerScope.Person ? QuerySearchLists.GetPersonListsSql : QuerySearchLists.GetGroupListsSql,
                p =>
                {
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                    p.AddWithValue(pn.ListType, listType is null ? DBNull.Value : (int)listType);
                },
                reader => scope == OwnerScope.Person ? reader.ToPersonSearchList() : reader.ToGroupSearchList());

            if (skeletons.Count == 0)
                return [];

            // One partition read for every name the picker needs, rather than a point read each.
            var contents = await _cqlExecutor.QueryManyAsync(
                scope == OwnerScope.Person
                    ? QuerySearchListsCql.GetAllForPersonSql
                    : QuerySearchListsCql.GetAllForGroupSql,
                p => p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId),
                row => scope == OwnerScope.Person
                    ? row.ToPersonSearchListContent()
                    : row.ToGroupSearchListContent());

            var byId = contents.ToDictionary(content => content.Id);

            foreach (var skeleton in skeletons)
                Hydrate(skeleton, byId.GetValueOrDefault(skeleton.Id));

            return skeletons;
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex,
                "GetAllAsync failed for search lists. Scope: [{Scope}] OwnerId: [{OwnerId}]", scope, ownerId);
            throw;
        }
    }

    public async Task<SearchList?> UpdateAsync(
        OwnerScope scope, int ownerId, Guid uuid, SearchListType listType, string name, SearchListPayload payload)
    {
        try
        {
            // The Postgres update is the authorization check as well as the timestamp touch: it
            // returns nothing when the list is not the owner's, and the blobs are never rewritten.
            var skeleton = await _sqlExecutor.QuerySingleAsync(
                scope == OwnerScope.Person ? QuerySearchLists.UpdatePersonSql : QuerySearchLists.UpdateGroupSql,
                p =>
                {
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonSearchListUuid : pn.GroupSearchListUuid, uuid);
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                    p.AddWithValue(pn.ListType, (int)listType);
                },
                reader => scope == OwnerScope.Person ? reader.ToPersonSearchList() : reader.ToGroupSearchList());

            if (skeleton is null)
                return null;

            await WriteContentAsync(scope, ownerId, skeleton.Id, name, payload);

            skeleton.Name = name;
            skeleton.Lines = payload.Lines;
            skeleton.References = payload.References;
            return skeleton;
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex,
                "UpdateAsync failed for a search list. Scope: [{Scope}] OwnerId: [{OwnerId}] Uuid: [{Uuid}]",
                scope, ownerId, uuid);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(OwnerScope scope, int ownerId, Guid uuid)
    {
        try
        {
            var id = await _sqlExecutor.QuerySingleValueAsync(
                scope == OwnerScope.Person ? QuerySearchLists.DeletePersonSql : QuerySearchLists.DeleteGroupSql,
                p =>
                {
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonSearchListUuid : pn.GroupSearchListUuid, uuid);
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                },
                reader => reader.GetInt32(0));

            if (id is null)
                return false;

            await _cqlExecutor.ExecuteAsync(
                scope == OwnerScope.Person
                    ? QuerySearchListsCql.DeletePersonSql
                    : QuerySearchListsCql.DeleteGroupSql,
                p =>
                {
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                    p.AddWithValue(scope == OwnerScope.Person ? pn.PersonSearchListId : pn.GroupSearchListId, id.Value);
                });

            return true;
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex,
                "DeleteAsync failed for a search list. Scope: [{Scope}] OwnerId: [{OwnerId}] Uuid: [{Uuid}]",
                scope, ownerId, uuid);
            throw;
        }
    }

    public async Task<IReadOnlyList<FileSearchResult>> SearchFilesAsync(
        OwnerScope scope,
        int ownerId,
        IReadOnlyList<Guid> listUuids,
        bool? isCurrent,
        IReadOnlyList<int> sourceMachineIds,
        int limit)
    {
        try
        {
            // Everything the caller owns, resolved in one pass. Reading the owner's own lists and
            // then selecting from them is what makes the authorization structural: a uuid the
            // caller does not own is simply not in this set, so it cannot be run. Checking
            // afterwards would be a rule someone could forget; this cannot be forgotten.
            var owned = await GetAllAsync(scope, ownerId);
            var byUuid = owned.ToDictionary(list => list.Uuid);

            var expanded = new List<IReadOnlyList<SearchListLine>>();

            foreach (var uuid in listUuids)
            {
                if (!byUuid.TryGetValue(uuid, out var list))
                    continue;

                if (list.ListType == SearchListType.And)
                {
                    // References are resolved now rather than trusted from when the AND list was
                    // saved: ownership can change underneath them.
                    foreach (var referenced in list.References)
                    {
                        if (byUuid.TryGetValue(referenced, out var target) && target.Lines.Count > 0)
                            expanded.Add(target.Lines);
                    }
                }
                else if (list.Lines.Count > 0)
                {
                    expanded.Add(list.Lines);
                }
            }

            var (sql, parameters) = QueryFileSearch.Build(expanded, isCurrent, sourceMachineIds, limit);

            return await _sqlExecutor.QueryManyAsync(
                sql,
                p =>
                {
                    foreach (var (name, value) in parameters)
                        p.AddWithValue(name, value);
                },
                reader => reader.ToFileSearchResult());
        }
        catch (Exception ex)
        {
            // The terms themselves are never logged: they are the thing encrypted at rest.
            _logger.WithCaller().LogError(ex,
                "SearchFilesAsync failed. Scope: [{Scope}] OwnerId: [{OwnerId}] Lists: [{Lists}]",
                scope, ownerId, listUuids.Count);
            throw;
        }
    }

    private Task<SearchList?> GetSkeletonAsync(OwnerScope scope, int ownerId, Guid uuid) =>
        _sqlExecutor.QuerySingleAsync(
            scope == OwnerScope.Person ? QuerySearchLists.GetPersonByUuidSql : QuerySearchLists.GetGroupByUuidSql,
            p =>
            {
                p.AddWithValue(scope == OwnerScope.Person ? pn.PersonSearchListUuid : pn.GroupSearchListUuid, uuid);
                p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
            },
            reader => scope == OwnerScope.Person ? reader.ToPersonSearchList() : reader.ToGroupSearchList());

    /// <summary>
    /// Encrypts and writes the content half. Plaintext goes in and ciphertext goes out, here and
    /// only here, so no caller can store a list in the clear by taking a different path.
    /// </summary>
    private Task WriteContentAsync(
        OwnerScope scope, int ownerId, int id, string name, SearchListPayload payload)
    {
        var serialized = JsonSerializer.Serialize(payload);

        return _cqlExecutor.ExecuteAsync(
            scope == OwnerScope.Person
                ? QuerySearchListsCql.UpsertPersonSql
                : QuerySearchListsCql.UpsertGroupSql,
            p =>
            {
                p.AddWithValue(scope == OwnerScope.Person ? pn.PersonId : pn.GroupId, ownerId);
                p.AddWithValue(scope == OwnerScope.Person ? pn.PersonSearchListId : pn.GroupSearchListId, id);
                p.AddWithValue(pn.Name, _cipher.Encrypt(name));
                p.AddWithValue(pn.Payload, _cipher.Encrypt(serialized));
                p.AddWithValue(pn.PayloadVersion, SearchList.CurrentPayloadVersion);
            });
    }

    /// <summary>
    /// Fills a skeleton from its Scylla half. Missing content is not an error the caller can act
    /// on -- it means a two-store write did not finish -- so the list comes back nameless and
    /// empty rather than throwing, and the gap is visible in the response instead of a 500.
    /// </summary>
    private void Hydrate(SearchList skeleton, SearchListContent? content)
    {
        if (content is null)
        {
            _logger.WithCaller().LogWarning(
                "A search list has no content row. Scope: [{Scope}] OwnerId: [{OwnerId}] Id: [{Id}]",
                skeleton.Scope, skeleton.OwnerId, skeleton.Id);
            return;
        }

        skeleton.PayloadVersion = content.PayloadVersion;
        skeleton.Name = _cipher.Decrypt(content.Name);

        var payload = JsonSerializer.Deserialize<SearchListPayload>(_cipher.Decrypt(content.Payload));
        skeleton.Lines = payload?.Lines ?? [];
        skeleton.References = payload?.References ?? [];
    }
}
