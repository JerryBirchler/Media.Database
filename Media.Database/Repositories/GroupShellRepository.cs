using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupShellRepository"/>
public class GroupShellRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<GroupShellRepository> logger)
    : IGroupShellRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<GroupShellRepository> _logger = logger.Initializer();

    public async Task<GroupShell> CreateAsync()
    {
        try
        {
            var result = await _sqlExecutor.QuerySingleAsync(
                QueryGroupShell.InsertSql,
                static _ => { },
                reader => reader.ToGroupShell());

            // InsertSql's RETURNING clause always produces exactly one row.
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed");
            throw;
        }
    }

    public async Task<GroupShell?> GetByIdAsync(int groupShellId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryGroupShell.GetByIdSql,
                p => p.AddWithValue(pn.Id, groupShellId),
                reader => reader.ToGroupShell());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetByIdAsync failed for GroupShellId: [{GroupShellId}]", groupShellId);
            throw;
        }
    }

    public async Task<GroupShell?> PromoteIfUnpromotedAsync(int groupShellId, int groupId)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryGroupShell.PromoteIfUnpromotedSql,
                p =>
                {
                    p.AddWithValue(pn.Id, groupShellId);
                    p.AddWithValue(pn.GroupId, groupId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                },
                reader => reader.ToGroupShell());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PromoteIfUnpromotedAsync failed for GroupShellId: [{GroupShellId}], GroupId: [{GroupId}]", groupShellId, groupId);
            throw;
        }
    }
}
