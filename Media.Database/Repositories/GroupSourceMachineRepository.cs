using Media.Common.Helpers.Fluent;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupSourceMachineRepository"/>
public class GroupSourceMachineRepository(
    ISqlQueryExecutor sqlExecutor,
    IMapGroupSourceMachineResponse groupSourceMachineResponseMapper,
    ILogger<GroupSourceMachineRepository> logger)
    : IGroupSourceMachineRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapGroupSourceMachineResponse _groupSourceMachineResponseMapper = groupSourceMachineResponseMapper;
    private readonly FluentLogger<GroupSourceMachineRepository> _logger = logger.Initializer();

    public async Task<GroupSourceMachine> UpsertAsync(int groupId, int sourceMachineId)
    {
        try
        {
            var result = await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupsSourceMachines.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupSourceMachine(_groupSourceMachineResponseMapper)
            );

            // UpsertSql's update/insert CTE pair always produces exactly one row between them.
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertAsync failed for GroupId {GroupId}, SourceMachineId {SourceMachineId}", groupId, sourceMachineId);
            throw;
        }
    }

    public async Task<GroupSourceMachine?> DeactivateAsync(int groupId, int sourceMachineId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupsSourceMachines.DeactivateSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.SourceMachineId, sourceMachineId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupSourceMachine(_groupSourceMachineResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeactivateAsync failed for GroupId {GroupId}, SourceMachineId {SourceMachineId}", groupId, sourceMachineId);
            throw;
        }
    }

    public async Task<List<(int SourceMachineId, string SourceMachineName)>> GetSourceMachineIdentifiersByGroupIdAsync(int groupId, bool includeInactive, (int SourceMachineId, string SourceMachineName)? next, int limit)
    {
        try
        {
            var afterSourceMachineName = next?.SourceMachineName;
            var afterSourceMachineId = next?.SourceMachineId;

            return await _sqlExecutor.QueryManyAsync(
                QueryGroupsSourceMachines.GetSourceMachineIdentifiersByGroupIdSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.IncludeInactive, includeInactive);
                    p.AddWithValue(pn.SourceMachineName, afterSourceMachineName.ToNullableValueForSql());
                    p.AddWithValue(pn.SourceMachineId, afterSourceMachineId.ToNullableValueForSql());
                    p.AddWithValue(pn.Limit, limit);
                },
                reader => reader.ToSourceMachineIdentifier());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetSourceMachineIdentifiersByGroupIdAsync failed for GroupId {GroupId}", groupId);
            throw;
        }
    }
}
