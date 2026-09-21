using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupEncryptionKeyRepository"/>
public class GroupEncryptionKeyRepository(
    ISqlQueryExecutor sqlExecutor,
    ILogger<GroupEncryptionKeyRepository> logger)
    : IGroupEncryptionKeyRepository
{
    private readonly ISqlQueryExecutor _sqlExecutor = sqlExecutor;
    private readonly FluentLogger<GroupEncryptionKeyRepository> _logger = logger.Initializer();

    public async Task<GroupEncryptionKey> CreateAsync(int groupShellId, EncryptionDataCategory dataCategory, string wrappedDek)
    {
        try
        {
            var result = await _sqlExecutor.QuerySingleAsync(
                QueryGroupEncryptionKeys.InsertSql,
                p =>
                {
                    p.AddWithValue(pn.GroupShellId, groupShellId);
                    p.AddWithValue(pn.DataCategory, (int)dataCategory);
                    p.AddWithValue(pn.WrappedDek, wrappedDek);
                },
                reader => reader.ToGroupEncryptionKey());

            // InsertSql's RETURNING clause always produces exactly one row.
            return result!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CreateAsync failed for GroupShellId {GroupShellId}, DataCategory {DataCategory}", groupShellId, dataCategory);
            throw;
        }
    }

    public async Task<int> DeactivateAllAsync(int groupShellId)
    {
        try
        {
            return await _sqlExecutor.ExecuteAsync(
                QueryGroupEncryptionKeys.DeactivateAllForShellSql,
                p =>
                {
                    p.AddWithValue(pn.GroupShellId, groupShellId);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeactivateAllAsync failed for GroupShellId {GroupShellId}", groupShellId);
            throw;
        }
    }

    public async Task<GroupEncryptionKey?> GetActiveAsync(int groupShellId, EncryptionDataCategory dataCategory)
    {
        try
        {
            return await _sqlExecutor.QuerySingleAsync(
                QueryGroupEncryptionKeys.GetActiveSql,
                p =>
                {
                    p.AddWithValue(pn.GroupShellId, groupShellId);
                    p.AddWithValue(pn.DataCategory, (int)dataCategory);
                },
                reader => reader.ToGroupEncryptionKey());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetActiveAsync failed for GroupShellId {GroupShellId}, DataCategory {DataCategory}", groupShellId, dataCategory);
            throw;
        }
    }
}
