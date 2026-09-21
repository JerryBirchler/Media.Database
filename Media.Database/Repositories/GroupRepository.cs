using Media.Common.Helpers.Fluent;
using Media.Common.Providers;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <summary>
/// PostgreSQL-backed implementation of <see cref="IGroupRepository"/>. Writes go to PostgreSQL
/// only -- Scylla is kept in sync separately and asynchronously by the CDC pipeline (see
/// Cdc.GroupsCdcSyncHandler), same split as <see cref="FileRepository"/>. <see cref="GetByIdsAsync"/>
/// hydrates preferring Scylla, falling back to PostgreSQL per row.
/// </summary>
public class GroupRepository(
    ISqlQueryExecutor sqlExecutor,
    ICqlQueryExecutor cqlExecutor,
    IScyllaSessionProvider scyllaProvider,
    IMapGroupResponse groupResponseMapper,
    ILogger<GroupRepository> logger)
    : BaseRepository(scyllaProvider), IGroupRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly IMapGroupResponse _groupResponseMapper = groupResponseMapper;
    private readonly FluentLogger<GroupRepository> _logger = logger.Initializer();

    public async Task<Group?> CreateAsync(string name, string title, string? description, bool isActive)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroups.AddGroupSql,
                p =>
                {
                    p.AddWithValue(pn.Name, name);
                    p.AddWithValue(pn.Title, title);
                    p.AddWithValue(pn.Description, (object?)description ?? DBNull.Value);
                    p.AddWithValue(pn.IsActive, isActive);
                },
                reader => reader.ToGroup(_groupResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed for Name: [{Name}]", name);
            throw;
        }
    }

    public async Task<Group?> GetByUuidAsync(Guid groupUuid)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroups.GetByUuidSql,
                p => p.AddWithValue(pn.GroupUuid, groupUuid),
                reader => reader.ToGroup(_groupResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByUuidAsync failed for GroupUuid: [{GroupUuid}]", groupUuid);
            throw;
        }
    }

    public async Task<Group?> GetByNameAsync(string name)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroups.GetByNameSql,
                p => p.AddWithValue(pn.Name, name),
                reader => reader.ToGroup(_groupResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByNameAsync failed for Name: [{Name}]", name);
            throw;
        }
    }

    public async Task<Group?> UpdateAsync(int groupId, string? title, string? description)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroups.UpdateGroupSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.Title, (object?)title ?? DBNull.Value);
                    p.AddWithValue(pn.Description, (object?)description ?? DBNull.Value);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroup(_groupResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<Group?> SetActiveAsync(int groupId, bool isActive)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroups.SetActiveSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.IsActive, isActive);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroup(_groupResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetActiveAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<Group?> SetIsEncryptedAsync(int groupId, bool isEncrypted)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroups.SetIsEncryptedSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.IsEncrypted, isEncrypted);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroup(_groupResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SetIsEncryptedAsync failed for GroupId: [{GroupId}]", groupId);
            throw;
        }
    }

    public async Task<List<Group>> GetByIdsAsync(IEnumerable<int> groupIds, int maxDegreeOfParallelism)
    {
        var results = new ConcurrentBag<Group>();

        await Parallel.ForEachAsync(
            groupIds,
            new ParallelOptions { MaxDegreeOfParallelism = maxDegreeOfParallelism },
            async (groupId, _) =>
            {
                var group = await GetByIdPreferringScyllaAsync(groupId);
                if (group is not null)
                    results.Add(group);
            });

        return [.. results];
    }

    /// <summary>
    /// Looks up a single group by id, preferring Scylla and falling back to PostgreSQL when
    /// Scylla has no row yet (CDC lag) or is unreachable. A Scylla failure here is deliberately
    /// not rethrown -- PostgreSQL is the durable source of truth, so a temporarily unavailable
    /// Scylla cluster should degrade this to a normal PostgreSQL read rather than fail it.
    /// </summary>
    private async Task<Group?> GetByIdPreferringScyllaAsync(int groupId)
    {
        var log = _logger.WithCaller();

        try
        {
            var fromScylla = await _cqlExecutor.QuerySingleAsync(
                QueryGroups.GetByIdCql,
                p => p.AddWithValue(pn.GroupId, groupId),
                row => row.ToGroup());

            if (fromScylla is not null)
                return fromScylla;
        }
        catch (Exception ex) when (IsScyllaConnectivityException(ex))
        {
            log.LogError(ex, "Scylla cluster unavailable fetching GroupId: [{GroupId}]; falling back to Postgres", groupId);
            await TryHealScyllaSessionAsync(_logger, nameof(GetByIdPreferringScyllaAsync));
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Scylla lookup failed for GroupId: [{GroupId}]; falling back to Postgres", groupId);
        }

        return await _sqlExecutor.QuerySingleAsync
        (
            QueryGroups.GetByIdSql,
            p => p.AddWithValue(pn.GroupId, groupId),
            reader => reader.ToGroup(_groupResponseMapper)
        );
    }
}
