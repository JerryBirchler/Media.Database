using Media.Common.Helpers.Fluent;
using Media.Database.Models;
using Media.Database.Repositories.Queries;
using Media.Database.Repositories.Queries.Helpers;
using Microsoft.Extensions.Logging;

#pragma warning disable CS8981
using pn = Media.Database.Repositories.Schemas.ParameterNames;
#pragma warning restore CS8981

namespace Media.Database.Repositories;

/// <inheritdoc cref="IGroupUuidOrchestrationRepository"/>
public class GroupUuidOrchestrationRepository(
    ICqlQueryExecutor cqlExecutor,
    ILogger<GroupUuidOrchestrationRepository> logger) : IGroupUuidOrchestrationRepository
{
    private readonly ICqlQueryExecutor _cqlExecutor = cqlExecutor;
    private readonly FluentLogger<GroupUuidOrchestrationRepository> _logger = logger.Initializer();

    public async Task UpsertAsync(Guid personUuid, int groupShellId, string encryptedUuidBlob, Guid groupEncryptionKeyUuid)
    {
        try
        {
            await _cqlExecutor.ExecuteAsync(
                QueryGroupUuidOrchestration.UpsertSql,
                p =>
                {
                    p.AddWithValue(pn.PersonUuid, personUuid);
                    p.AddWithValue(pn.GroupShellId, groupShellId);
                    p.AddWithValue(pn.EncryptedUuidBlob, encryptedUuidBlob);
                    p.AddWithValue(pn.GroupEncryptionKeyUuid, groupEncryptionKeyUuid);
                    p.AddWithValue(pn.UpdatedOn, DateTimeOffset.UtcNow);
                });
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "UpsertAsync failed for PersonUuid {PersonUuid}, GroupShellId {GroupShellId}", personUuid, groupShellId);
            throw;
        }
    }

    public async Task<List<GroupUuidOrchestration>> GetAllByPersonUuidAsync(Guid personUuid)
    {
        try
        {
            return await _cqlExecutor.QueryManyAsync(
                QueryGroupUuidOrchestration.GetAllByPersonUuidSql,
                p => p.AddWithValue(pn.PersonUuid, personUuid),
                row => row.ToGroupUuidOrchestration());
        }
        catch (Exception ex)
        {
            _logger.WithCaller().LogError(ex, "GetAllByPersonUuidAsync failed for PersonUuid {PersonUuid}", personUuid);
            throw;
        }
    }
}
