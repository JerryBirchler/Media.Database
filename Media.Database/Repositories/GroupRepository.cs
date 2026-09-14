using Media.Common.Helpers.Fluent;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupRepository"/>
public class GroupRepository(
    ISqlQueryExecutor sqlExecutor,
    IMapGroupResponse groupResponseMapper,
    ILogger<GroupRepository> logger)
    : IGroupRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
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
            _logger.LogError(ex, "CreateAsync failed for Name {Name}", name);
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
            _logger.LogError(ex, "GetByUuidAsync failed for GroupUuid {GroupUuid}", groupUuid);
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
            _logger.LogError(ex, "GetByNameAsync failed for Name {Name}", name);
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
            _logger.LogError(ex, "UpdateAsync failed for GroupId {GroupId}", groupId);
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
            _logger.LogError(ex, "SetActiveAsync failed for GroupId {GroupId}", groupId);
            throw;
        }
    }
}
