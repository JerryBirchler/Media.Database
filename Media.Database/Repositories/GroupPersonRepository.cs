using Media.Common.Helpers.Fluent;
using Media.Database.Mappers;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupPersonRepository"/>
public class GroupPersonRepository(
    ISqlQueryExecutor sqlExecutor,
    IMapGroupPersonResponse groupPersonResponseMapper,
    ILogger<GroupPersonRepository> logger)
    : IGroupPersonRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly IMapGroupPersonResponse _groupPersonResponseMapper = groupPersonResponseMapper;
    private readonly FluentLogger<GroupPersonRepository> _logger = logger.Initializer();

    public async Task<GroupPerson> UpsertAsync(int groupId, int personId, bool isAdmin)
    {
        try
        {
            var result = await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupsPersons.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.IsAdmin, isAdmin);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupPerson(_groupPersonResponseMapper)
            );

            // UpsertSql's update/insert CTE pair always produces exactly one row between them.
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpsertAsync failed for GroupId {GroupId}, PersonId {PersonId}", groupId, personId);
            throw;
        }
    }

    public async Task<GroupPerson?> GetActiveAsync(int groupId, int personId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupsPersons.GetActiveSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.PersonId, personId);
                },
                reader => reader.ToGroupPerson(_groupPersonResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveAsync failed for GroupId {GroupId}, PersonId {PersonId}", groupId, personId);
            throw;
        }
    }

    public async Task<GroupPerson?> DeactivateAsync(int groupId, int personId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync
            (
                QueryGroupsPersons.DeactivateSql,
                p =>
                {
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.PersonId, personId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupPerson(_groupPersonResponseMapper)
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeactivateAsync failed for GroupId {GroupId}, PersonId {PersonId}", groupId, personId);
            throw;
        }
    }

    public async Task<int> CountActiveAdminsAsync(int groupId)
    {
        try
        {
            var count = await _sqlExecutor.QuerySingleValueAsync
            (
                QueryGroupsPersons.CountActiveAdminsSql,
                p => p.AddWithValue(pn.GroupId, groupId),
                reader => reader.GetInt64(0)
            );

            return (int)(count ?? 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CountActiveAdminsAsync failed for GroupId {GroupId}", groupId);
            throw;
        }
    }
}
